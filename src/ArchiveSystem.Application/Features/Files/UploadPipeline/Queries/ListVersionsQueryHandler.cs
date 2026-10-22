using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public class ListVersionsQueryHandler : IRequestHandler<ListVersionsQuery, IReadOnlyList<FileVersionDto>>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public ListVersionsQueryHandler(
            IFileRepository fileRepository,
            IFileVersionRepository fileVersionRepository,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _fileRepository = fileRepository;
            _fileVersionRepository = fileVersionRepository;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<IReadOnlyList<FileVersionDto>> Handle(ListVersionsQuery request, CancellationToken cancellationToken)
        {
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            if (file == null)
            {
                throw new NotFoundException($"File '{request.FileId}' not found.");
            }

            await _fileAccessAuthorizer.EnsureCanViewFileAsync(request.FileId, cancellationToken);

            var versions = await _fileVersionRepository.ListVersionsAsync(request.FileId, cancellationToken);

            return versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new FileVersionDto
                {
                    VersionId = v.Id,
                    VersionNumber = v.VersionNumber,
                    Size = v.Size,
                    ContentType = v.ContentType,
                    Sha256 = v.Sha256,
                    StoragePath = v.StoragePath,
                    FileName = v.FileName,
                    IsDeleted = v.IsDeleted,
                    ThumbnailStatus = (int)v.ThumbnailStatus,
                    OcrStatus = (int)v.OcrStatus,
                    OcrError = v.OcrError,
                    CreatedAt = v.CreatedAt,
                    DeletedAt = v.DeletedAt
                })
                .ToList();
        }
    }
}
