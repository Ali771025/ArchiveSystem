using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using DomainFile = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class InitiateUploadSessionCommandHandler : IRequestHandler<InitiateUploadSessionCommand, InitiateUploadSessionResponse>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUploadSessionRepository _uploadSessionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;
        private readonly FileUploadOptions _uploadOptions;
        private readonly ObjectStorageOptions _storageOptions;

        public InitiateUploadSessionCommandHandler(
            IFileRepository fileRepository,
            IUploadSessionRepository uploadSessionRepository,
            IObjectStorage objectStorage,
            IAuditService auditService,
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IFileAccessAuthorizer fileAccessAuthorizer,
            IOptions<FileUploadOptions> uploadOptions,
            IOptions<ObjectStorageOptions> storageOptions)
        {
            _fileRepository = fileRepository;
            _uploadSessionRepository = uploadSessionRepository;
            _objectStorage = objectStorage;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _fileAccessAuthorizer = fileAccessAuthorizer;
            _uploadOptions = uploadOptions.Value;
            _storageOptions = storageOptions.Value;
        }

        public async Task<InitiateUploadSessionResponse> Handle(InitiateUploadSessionCommand request, CancellationToken cancellationToken)
        {
            var tenantId = string.IsNullOrWhiteSpace(request.TenantId)
                ? _uploadOptions.DefaultTenantId
                : request.TenantId!.Trim();
            var targetClassificationId = await _fileAccessAuthorizer.ResolveInitiateUploadClassificationAsync(
                request.CaseId,
                request.ClassificationId,
                cancellationToken);

            var sessionId = Guid.NewGuid();
            DomainFile? file = null;
            UploadSession? session = null;
            string objectKey = string.Empty;
            string uploadId = string.Empty;
            var transactionCommitted = false;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    file = await ResolveOrCreateFileAsync(request, ct);
                    objectKey = BuildObjectKey(tenantId, file.Id, sessionId);

                    await _objectStorage.EnsureBucketExistsAsync(_storageOptions.BucketName, ct);
                    uploadId = await _objectStorage.InitiateMultipartUploadAsync(
                        _storageOptions.BucketName,
                        objectKey,
                        request.ContentType,
                        ct);

                    session = new UploadSession(
                        id: sessionId,
                        fileId: file.Id,
                        caseId: request.CaseId ?? file.CaseId,
                        tenantId: tenantId,
                        originalFileName: request.OriginalFileName,
                        contentType: request.ContentType,
                        expectedSizeBytes: request.ExpectedSizeBytes,
                        expectedSha256: request.ExpectedSha256,
                        partSizeBytes: _uploadOptions.DefaultPartSizeBytes,
                        bucketName: _storageOptions.BucketName,
                        objectKey: objectKey,
                        multipartUploadId: uploadId);

                    await _uploadSessionRepository.AddAsync(session, ct);
                    await _uploadSessionRepository.SaveChangesAsync(ct);
                }, cancellationToken);

                transactionCommitted = true;
                if (file == null || session == null)
                {
                    throw new InvalidOperationException("Failed to initialize upload session.");
                }

                await LogAuditAsync(
                    action: "UPLOAD_INITIATED",
                    tableName: "UploadSessions",
                    recordId: session.Id.ToString(),
                    newValue: System.Text.Json.JsonSerializer.Serialize(new
                    {
                        session.Id,
                        session.FileId,
                        session.ObjectKey,
                        session.ExpectedSizeBytes,
                        session.PartSizeBytes,
                        session.Status
                    }));

                return new InitiateUploadSessionResponse
                {
                    SessionId = session.Id,
                    FileId = file.Id,
                    PartSizeBytes = session.PartSizeBytes,
                    Status = session.Status.ToString(),
                    CreatedAt = session.CreatedAt
                };
            }
            catch
            {
                // Multipart upload exists in object storage; best-effort cleanup avoids orphaned parts.
                if (!transactionCommitted && !string.IsNullOrWhiteSpace(uploadId))
                {
                    await _objectStorage.AbortMultipartUploadAsync(
                        _storageOptions.BucketName,
                        objectKey,
                        uploadId,
                        cancellationToken);
                }

                throw;
            }
        }

        private async Task<DomainFile> ResolveOrCreateFileAsync(InitiateUploadSessionCommand request, CancellationToken cancellationToken)
        {
            if (request.FileId.HasValue)
            {
                var existingFile = await _fileRepository.GetByIdAsync(request.FileId.Value, cancellationToken);
                if (existingFile == null)
                {
                    throw new NotFoundException($"File '{request.FileId.Value}' was not found.");
                }

                return existingFile;
            }

            if (request.CaseId.HasValue)
            {
                var existingCaseFile = await _fileRepository.FindActiveByCaseAndNameAsync(
                    request.CaseId.Value,
                    request.OriginalFileName,
                    cancellationToken);
                if (existingCaseFile != null)
                {
                    return existingCaseFile;
                }
            }

            var extension = Path.GetExtension(request.OriginalFileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            var file = new DomainFile(
                id: 0,
                name: request.OriginalFileName,
                size: FileSize.Create(request.ExpectedSizeBytes),
                extension: FileExtension.Create(extension),
                contentType: request.ContentType,
                storagePath: $"pending/{Guid.NewGuid():N}",
                caseId: request.CaseId);

            await _fileRepository.AddAsync(file, cancellationToken);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            return file;
        }

        private static string BuildObjectKey(string tenantId, long fileId, Guid sessionId)
        {
            var randomPrefix = Guid.NewGuid().ToString("N")[..12];
            return $"tenant/{tenantId}/files/{fileId}/versions/{sessionId:N}/{randomPrefix}";
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
