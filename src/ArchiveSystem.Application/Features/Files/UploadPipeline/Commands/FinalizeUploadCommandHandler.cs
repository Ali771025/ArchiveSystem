using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class FinalizeUploadCommandHandler : IRequestHandler<FinalizeUploadCommand, FinalizeUploadResponse>
    {
        private readonly IUploadSessionRepository _uploadSessionRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly IFileContentStore _fileContentStore;
        private readonly IAuditService _auditService;
        private readonly IMessageBus _messageBus;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public FinalizeUploadCommandHandler(
            IUploadSessionRepository uploadSessionRepository,
            IFileRepository fileRepository,
            IFileVersionRepository fileVersionRepository,
            IObjectStorage objectStorage,
            IFileContentStore fileContentStore,
            IAuditService auditService,
            IMessageBus messageBus,
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _uploadSessionRepository = uploadSessionRepository;
            _fileRepository = fileRepository;
            _fileVersionRepository = fileVersionRepository;
            _objectStorage = objectStorage;
            _fileContentStore = fileContentStore;
            _auditService = auditService;
            _messageBus = messageBus;
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<FinalizeUploadResponse> Handle(FinalizeUploadCommand request, CancellationToken cancellationToken)
        {
            var session = await _uploadSessionRepository.GetByIdAsync(request.SessionId, includeParts: true, cancellationToken);
            if (session == null)
            {
                throw new NotFoundException($"Upload session '{request.SessionId}' not found.");
            }

            await _fileAccessAuthorizer.EnsureCanWriteSessionAsync(session, cancellationToken);

            if (session.Status == UploadSessionStatus.Completed && session.FinalizedVersionId.HasValue)
            {
                var completedVersion = await _fileVersionRepository.GetByIdAsync(session.FinalizedVersionId.Value, cancellationToken);
                if (completedVersion == null)
                {
                    throw new NotFoundException($"Completed file version '{session.FinalizedVersionId.Value}' not found.");
                }

                return new FinalizeUploadResponse
                {
                    FileId = completedVersion.FileId,
                    VersionId = completedVersion.Id,
                    VersionNumber = completedVersion.VersionNumber,
                    Sha256Checksum = completedVersion.Sha256,
                    SizeBytes = completedVersion.Size,
                    StoragePath = completedVersion.StoragePath,
                    AlreadyFinalized = true
                };
            }

            if (session.Status == UploadSessionStatus.Aborted)
            {
                throw new ConflictException("Upload session was aborted.");
            }

            if (session.Status == UploadSessionStatus.Finalizing)
            {
                throw new ConflictException("Upload session is currently finalizing.");
            }

            var parts = (await _uploadSessionRepository.ListPartsAsync(session.Id, cancellationToken))
                .OrderBy(p => p.PartNumber)
                .ToList();

            if (parts.Count == 0)
            {
                throw new ValidationException("No uploaded parts found for this session.");
            }

            var expectedPartCount = (int)Math.Ceiling(session.ExpectedSizeBytes / (double)session.PartSizeBytes);
            var missingPartNumbers = Enumerable.Range(1, expectedPartCount)
                .Where(expected => parts.All(actual => actual.PartNumber != expected))
                .ToArray();

            if (missingPartNumbers.Length > 0)
            {
                throw new ValidationException($"Cannot finalize upload. Missing parts: {string.Join(",", missingPartNumbers)}");
            }

            var transactionCommitted = false;
            FileVersion? fileVersion = null;
            ArchiveFile? file = null;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    session.MarkFinalizing();
                    await _uploadSessionRepository.UpdateAsync(session, ct);
                    await _uploadSessionRepository.SaveChangesAsync(ct);

                    var completedParts = parts
                        .Select(p => new MultipartPartETag(p.PartNumber, p.ETag))
                        .ToList();

                    await _objectStorage.CompleteMultipartUploadAsync(
                        session.BucketName,
                        session.ObjectKey,
                        session.MultipartUploadId,
                        completedParts,
                        ct);

                    var objectStat = await _objectStorage.GetObjectStatAsync(
                        session.BucketName,
                        session.ObjectKey,
                        ct);

                    if (objectStat.SizeBytes != session.ExpectedSizeBytes)
                    {
                        session.MarkFailed($"Uploaded size mismatch. Expected={session.ExpectedSizeBytes}, Actual={objectStat.SizeBytes}");
                        await _uploadSessionRepository.UpdateAsync(session, ct);
                        await _uploadSessionRepository.SaveChangesAsync(ct);
                        throw new ValidationException(session.FailureReason!);
                    }

                    var calculatedSha256 = await _fileContentStore.ComputeSha256Async(
                        session.BucketName,
                        session.ObjectKey,
                        ct);

                    if (!string.Equals(calculatedSha256, session.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        session.MarkFailed("SHA-256 checksum mismatch.");
                        await _uploadSessionRepository.UpdateAsync(session, ct);
                        await _uploadSessionRepository.SaveChangesAsync(ct);
                        throw new ValidationException("SHA-256 checksum mismatch.");
                    }

                    if (!session.FileId.HasValue)
                    {
                        throw new ValidationException("Upload session is missing a file reference.");
                    }

                    file = await _fileRepository.GetByIdAsync(session.FileId.Value, ct);
                    if (file == null)
                    {
                        throw new NotFoundException($"File '{session.FileId.Value}' not found.");
                    }

                    var nextVersionNumber = await _fileVersionRepository.GetLatestVersionNumberAsync(file.Id, ct) + 1;
                    fileVersion = new FileVersion(
                        id: 0,
                        fileId: file.Id,
                        versionNumber: nextVersionNumber,
                        storagePath: session.ObjectKey,
                        fileName: session.OriginalFileName,
                        contentType: session.ContentType,
                        size: session.ExpectedSizeBytes,
                        sha256: calculatedSha256);

                    await _fileVersionRepository.CreateVersionAsync(fileVersion, ct);
                    await _fileVersionRepository.SaveChangesAsync(ct);

                    file.ApplyLatestVersionMetadata(
                        fileVersion.Id,
                        fileVersion.StoragePath,
                        fileVersion.Size,
                        fileVersion.ContentType,
                        fileVersion.Sha256);

                    await _fileRepository.UpdateAsync(file, ct);
                    await _fileRepository.SaveChangesAsync(ct);

                    session.MarkCompleted(fileVersion.Id);
                    await _uploadSessionRepository.UpdateAsync(session, ct);
                    await _uploadSessionRepository.SaveChangesAsync(ct);
                }, cancellationToken);

                transactionCommitted = true;
                if (file == null || fileVersion == null)
                {
                    throw new InvalidOperationException("Failed to finalize upload session.");
                }

                await LogAuditAsync(
                    action: "UPLOAD_FINALIZED",
                    tableName: "FileVersions",
                    recordId: fileVersion.Id.ToString(),
                    newValue: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        sessionId = session.Id,
                        fileId = file.Id,
                        caseId = file.CaseId,
                        classificationId = file.Case?.ClassificationId,
                        versionId = fileVersion.Id,
                        version = fileVersion.VersionNumber,
                        sha256 = fileVersion.Sha256,
                        size = fileVersion.Size,
                        storagePath = fileVersion.StoragePath
                    }));

                await _messageBus.PublishAsync(
                    new FileVersionCreatedEvent(file.Id, fileVersion.Id, fileVersion.StoragePath, fileVersion.ContentType, fileVersion.Size),
                    cancellationToken);

                return new FinalizeUploadResponse
                {
                    FileId = file.Id,
                    VersionId = fileVersion.Id,
                    VersionNumber = fileVersion.VersionNumber,
                    Sha256Checksum = fileVersion.Sha256,
                    SizeBytes = fileVersion.Size,
                    StoragePath = fileVersion.StoragePath,
                    AlreadyFinalized = false
                };
            }
            catch (Exception ex)
            {
                if (!transactionCommitted)
                {
                    session.MarkFailed(ex.Message);
                    await _uploadSessionRepository.UpdateAsync(session, cancellationToken);
                    await _uploadSessionRepository.SaveChangesAsync(cancellationToken);
                }

                throw;
            }
        }

        private async Task LogAuditAsync(string action, string tableName, string recordId, string? newValue = null, string? oldValue = null)
        {
            var userId = ParseUserId();
            await _auditService.LogOperationAsync(
                userId,
                _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System",
                action,
                tableName,
                recordId,
                oldValue,
                newValue,
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown");
        }

        private long ParseUserId()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            var userIdRaw =
                claimsPrincipal?.FindFirst("sub")?.Value ??
                claimsPrincipal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdRaw, out var userId) ? userId : 0;
        }
    }
}
