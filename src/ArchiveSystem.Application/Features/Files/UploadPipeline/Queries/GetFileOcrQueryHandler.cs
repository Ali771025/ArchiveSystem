using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public sealed class GetFileOcrQueryHandler : IRequestHandler<GetFileOcrQuery, string?>
    {
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public GetFileOcrQueryHandler(
            IFileVersionRepository fileVersionRepository,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _fileVersionRepository = fileVersionRepository;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<string?> Handle(GetFileOcrQuery request, CancellationToken cancellationToken)
        {
            await _fileAccessAuthorizer.EnsureCanViewFileAsync(request.FileId, cancellationToken);

            var version = request.VersionId.HasValue
                ? await _fileVersionRepository.GetByIdAsync(request.VersionId.Value, cancellationToken)
                : await _fileVersionRepository.GetLatestVersionAsync(request.FileId, cancellationToken);

            if (version == null || version.FileId != request.FileId || version.IsDeleted)
            {
                return null;
            }

            var ocrVersion = !string.IsNullOrWhiteSpace(version.OcrResult)
                ? version
                : request.VersionId.HasValue
                    ? null
                    : await _fileVersionRepository.GetLatestVersionWithOcrAsync(request.FileId, cancellationToken);

            return string.IsNullOrWhiteSpace(ocrVersion?.OcrResult)
                ? null
                : ocrVersion.OcrResult;
        }
    }
}
