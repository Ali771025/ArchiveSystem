using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class AbortUploadCommandHandler : IRequestHandler<AbortUploadCommand, Unit>
    {
        private readonly IUploadSessionRepository _uploadSessionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public AbortUploadCommandHandler(
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

        public async Task<Unit> Handle(AbortUploadCommand request, CancellationToken cancellationToken)
        {
            var session = await _uploadSessionRepository.GetByIdAsync(request.SessionId, includeParts: false, cancellationToken);
            if (session == null)
            {
                throw new NotFoundException($"Upload session '{request.SessionId}' not found.");
            }

            await _fileAccessAuthorizer.EnsureCanWriteSessionAsync(session, cancellationToken);

            if (session.Status == UploadSessionStatus.Aborted)
            {
                return Unit.Value;
            }

            if (session.Status == UploadSessionStatus.Completed)
            {
                throw new ConflictException("Completed upload session cannot be aborted.");
            }

            await _objectStorage.AbortMultipartUploadAsync(
                session.BucketName,
                session.ObjectKey,
                session.MultipartUploadId,
                cancellationToken);

            session.MarkAborted(request.Reason);
            await _uploadSessionRepository.UpdateAsync(session, cancellationToken);
            await _uploadSessionRepository.SaveChangesAsync(cancellationToken);

            await LogAuditAsync(
                action: "UPLOAD_ABORTED",
                tableName: "UploadSessions",
                recordId: session.Id.ToString(),
                newValue: System.Text.Json.JsonSerializer.Serialize(new { session.Status, session.FailureReason }));

            return Unit.Value;
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
