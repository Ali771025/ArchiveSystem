using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public class GetDownloadQueryHandler : IRequestHandler<GetDownloadQuery, DownloadFileResult?>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IObjectStorage _objectStorage;
        private readonly ObjectStorageOptions _objectStorageOptions;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;
        private readonly IAccessControlLookupRepository _lookupRepository;
        private readonly IAuditWriter _auditWriter;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetDownloadQueryHandler(
            IFileRepository fileRepository,
            IFileVersionRepository fileVersionRepository,
            IObjectStorage objectStorage,
            IOptions<ObjectStorageOptions> objectStorageOptions,
            IFileAccessAuthorizer fileAccessAuthorizer,
            IAccessControlLookupRepository lookupRepository,
            IAuditWriter auditWriter,
            IHttpContextAccessor httpContextAccessor)
        {
            _fileRepository = fileRepository;
            _fileVersionRepository = fileVersionRepository;
            _objectStorage = objectStorage;
            _objectStorageOptions = objectStorageOptions.Value;
            _fileAccessAuthorizer = fileAccessAuthorizer;
            _lookupRepository = lookupRepository;
            _auditWriter = auditWriter;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DownloadFileResult?> Handle(GetDownloadQuery request, CancellationToken cancellationToken)
        {
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            if (file == null)
            {
                return null;
            }

            await _fileAccessAuthorizer.EnsureCanViewFileAsync(request.FileId, cancellationToken);

            Domain.Entities.FileVersion? version;
            if (request.VersionId.HasValue)
            {
                version = await _fileVersionRepository.GetByIdAsync(request.VersionId.Value, cancellationToken);
                if (version == null || version.FileId != request.FileId || version.IsDeleted)
                {
                    return null;
                }
            }
            else
            {
                version = await _fileVersionRepository.GetLatestVersionAsync(request.FileId, cancellationToken);
                if (version == null)
                {
                    return null;
                }
            }

            var stream = await _objectStorage.GetObjectStreamAsync(_objectStorageOptions.BucketName, version.StoragePath, cancellationToken);

            var descriptor = await _lookupRepository.GetFileAccessDescriptorAsync(request.FileId, cancellationToken);
            await _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = _fileAccessAuthorizer.CurrentUserId,
                ActorName = _httpContextAccessor.HttpContext?.User.Identity?.Name,
                ActionType = "FILE_DOWNLOADED",
                EntityType = "File",
                EntityId = request.FileId.ToString(),
                ClassificationId = descriptor?.ClassificationId,
                CaseId = file.CaseId,
                FileId = request.FileId,
                FileVersionId = version.Id,
                Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString(),
                AfterJson = System.Text.Json.JsonSerializer.Serialize(new { fileId = request.FileId, versionId = version.Id })
            }, cancellationToken);

            return new DownloadFileResult
            {
                Content = stream,
                FileName = version.FileName,
                ContentType = version.ContentType,
                FileId = file.Id,
                VersionId = version.Id
            };
        }
    }
}
