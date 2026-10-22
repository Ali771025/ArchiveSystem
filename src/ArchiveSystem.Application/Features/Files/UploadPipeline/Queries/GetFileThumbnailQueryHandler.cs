using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public sealed class GetFileThumbnailQueryHandler : IRequestHandler<GetFileThumbnailQuery, FileContentResult?>
    {
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public GetFileThumbnailQueryHandler(
            IFileVersionRepository fileVersionRepository,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _fileVersionRepository = fileVersionRepository;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<FileContentResult?> Handle(GetFileThumbnailQuery request, CancellationToken cancellationToken)
        {
            await _fileAccessAuthorizer.EnsureCanViewFileAsync(request.FileId, cancellationToken);

            var version = request.VersionId.HasValue
                ? await _fileVersionRepository.GetByIdAsync(request.VersionId.Value, cancellationToken)
                : await _fileVersionRepository.GetLatestVersionAsync(request.FileId, cancellationToken);

            if (version == null || version.FileId != request.FileId || version.IsDeleted)
            {
                return null;
            }

            var thumbnailVersion = version.Thumbnail is { Length: > 0 }
                ? version
                : await _fileVersionRepository.GetLatestVersionWithThumbnailAsync(request.FileId, cancellationToken);

            if (thumbnailVersion?.Thumbnail is not { Length: > 0 })
            {
                return null;
            }

            return new FileContentResult
            {
                Content = thumbnailVersion.Thumbnail,
                ContentType = string.IsNullOrWhiteSpace(thumbnailVersion.ThumbnailContentType)
                    ? "image/jpeg"
                    : thumbnailVersion.ThumbnailContentType
            };
        }
    }
}
