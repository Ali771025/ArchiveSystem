using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Enums;
using MediatR;

namespace ArchiveSystem.Application.Features.Files.Commands.RetryFileOcr
{
    public sealed class RetryFileOcrCommandHandler : IRequestHandler<RetryFileOcrCommand>
    {
        private readonly IFileVersionRepository _fileVersionRepository;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public RetryFileOcrCommandHandler(
            IFileVersionRepository fileVersionRepository,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _fileVersionRepository = fileVersionRepository;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task Handle(RetryFileOcrCommand request, CancellationToken cancellationToken)
        {
            await _fileAccessAuthorizer.EnsureCanWriteFileAsync(request.FileId, cancellationToken);

            var version = await _fileVersionRepository.GetByIdAsync(request.VersionId, cancellationToken);
            if (version is null || version.FileId != request.FileId || version.IsDeleted)
            {
                throw new NotFoundException($"File version '{request.VersionId}' was not found.");
            }

            if (version.OcrStatus != ProcessingStatus.Failed)
            {
                throw new ConflictException("Only failed OCR jobs can be queued for retry.");
            }

            if (!await _fileVersionRepository.TryQueueOcrRetryAsync(request.VersionId, cancellationToken))
            {
                throw new ConflictException("OCR status changed before the retry request was completed.");
            }
        }
    }
}
