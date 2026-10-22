using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class SoftDeleteFileVersionCommandHandler : IRequestHandler<SoftDeleteFileVersionCommand, Unit>
    {
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IAuditService _auditService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public SoftDeleteFileVersionCommandHandler(
            IFileVersionRepository fileVersionRepository,
            IFileRepository fileRepository,
            IAuditService auditService,
            IHttpContextAccessor httpContextAccessor,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _fileVersionRepository = fileVersionRepository;
            _fileRepository = fileRepository;
            _auditService = auditService;
            _httpContextAccessor = httpContextAccessor;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<Unit> Handle(SoftDeleteFileVersionCommand request, CancellationToken cancellationToken)
        {
            var version = await _fileVersionRepository.GetByIdAsync(request.VersionId, cancellationToken);
            if (version == null || version.FileId != request.FileId)
            {
                throw new NotFoundException($"File version '{request.VersionId}' was not found for file '{request.FileId}'.");
            }

            await _fileAccessAuthorizer.EnsureCanWriteFileAsync(request.FileId, cancellationToken);

            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken)
                ?? throw new NotFoundException($"File '{request.FileId}' was not found.");
            if (file.IsDeleted || file.Status == FileStatus.Deleted)
            {
                throw new ValidationException("سند حذف‌شده قابل تغییر نیست.");
            }

            if (version.IsDeleted)
            {
                return Unit.Value;
            }

            version.SoftDelete();
            await _fileVersionRepository.UpdateAsync(version, cancellationToken);
            await _fileVersionRepository.SaveChangesAsync(cancellationToken);

            var userId = ParseUserId();
            await _auditService.LogOperationAsync(
                userId,
                _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System",
                "FILE_VERSION_SOFT_DELETE",
                "FileVersions",
                version.Id.ToString(),
                null,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    version.FileId,
                    version.VersionNumber,
                    version.DeletedAt,
                    version.StoragePath
                }),
                _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown");

            return Unit.Value;
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
