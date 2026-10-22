using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class UploadPartCommandHandler : IRequestHandler<UploadPartCommand, UploadPartResponse>
    {
        private readonly IUploadSessionRepository _uploadSessionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public UploadPartCommandHandler(
            IUploadSessionRepository uploadSessionRepository,
            IObjectStorage objectStorage,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _uploadSessionRepository = uploadSessionRepository;
            _objectStorage = objectStorage;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<UploadPartResponse> Handle(UploadPartCommand request, CancellationToken cancellationToken)
        {
            var session = await _uploadSessionRepository.GetByIdAsync(request.SessionId, includeParts: false, cancellationToken);
            if (session == null)
            {
                throw new NotFoundException($"Upload session '{request.SessionId}' not found.");
            }

            await _fileAccessAuthorizer.EnsureCanWriteSessionAsync(session, cancellationToken);

            if (session.Status == UploadSessionStatus.Completed)
            {
                var existingPart = (await _uploadSessionRepository.ListPartsAsync(session.Id, cancellationToken))
                    .FirstOrDefault(p => p.PartNumber == request.PartNumber);

                if (existingPart != null)
                {
                    return new UploadPartResponse
                    {
                        PartNumber = existingPart.PartNumber,
                        ETag = existingPart.ETag
                    };
                }

                throw new ConflictException("Upload session already completed.");
            }

            if (session.Status == UploadSessionStatus.Aborted)
            {
                throw new ConflictException("Upload session was aborted.");
            }

            var maxPartNumber = (int)Math.Ceiling(session.ExpectedSizeBytes / (double)session.PartSizeBytes);
            if (request.PartNumber > maxPartNumber)
            {
                throw new ValidationException($"Part number '{request.PartNumber}' exceeds maximum expected parts '{maxPartNumber}'.");
            }

            session.MarkUploading();
            await _uploadSessionRepository.UpdateAsync(session, cancellationToken);

            var uploaded = await _objectStorage.UploadPartAsync(
                session.BucketName,
                session.ObjectKey,
                session.MultipartUploadId,
                request.PartNumber,
                request.Content,
                request.ContentLength,
                cancellationToken);

            var partEntity = new UploadPart(
                id: 0,
                sessionId: session.Id,
                partNumber: request.PartNumber,
                eTag: uploaded.ETag,
                sizeBytes: request.ContentLength);

            await _uploadSessionRepository.UpsertPartAsync(partEntity, cancellationToken);
            await _uploadSessionRepository.SaveChangesAsync(cancellationToken);

            await LogAuditAsync(
                "UPLOAD_PART",
                "UploadParts",
                $"{session.Id}:{request.PartNumber}",
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    session.Id,
                    request.PartNumber,
                    uploaded.ETag,
                    request.ContentLength
                }));

            return new UploadPartResponse
            {
                PartNumber = request.PartNumber,
                ETag = uploaded.ETag
            };
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
