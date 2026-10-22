// src/ArchiveSystem.Application/Features/Files/Commands/DeleteFile/DeleteFileCommandHandler.cs
using MediatR;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;

namespace ArchiveSystem.Application.Features.Files.Commands.DeleteFile
{
    public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, Unit>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public DeleteFileCommandHandler(
            IFileRepository fileRepository,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _fileRepository = fileRepository;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<Unit> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
        {
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            
            if (file == null)
            {
                throw new NotFoundException($"File with ID {request.FileId} not found.");
            }

            await _fileAccessAuthorizer.EnsureCanWriteFileAsync(request.FileId, cancellationToken);

            if (file.IsDeleted || file.Status == FileStatus.Deleted)
            {
                throw new ValidationException("این سند قبلاً حذف شده است.");
            }

            await _fileRepository.DeleteAsync(file, cancellationToken);
            await _fileRepository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
