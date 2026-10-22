// src/ArchiveSystem.Application/Features/Files/Queries/GetFileById/GetFileByIdQueryHandler.cs
using MediatR;
using ArchiveSystem.Domain.Repositories;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Files.Security;

namespace ArchiveSystem.Application.Features.Files.Queries.GetFileById
{
    public class GetFileByIdQueryHandler : IRequestHandler<GetFileByIdQuery, FileDto?>
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;

        public GetFileByIdQueryHandler(
            IFileRepository fileRepository,
            IFileAccessAuthorizer fileAccessAuthorizer)
        {
            _fileRepository = fileRepository;
            _fileAccessAuthorizer = fileAccessAuthorizer;
        }

        public async Task<FileDto?> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
        {
            var file = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
            
            if (file == null)
            {
                return null;
            }

            await _fileAccessAuthorizer.EnsureCanViewFileAsync(request.FileId, cancellationToken);

            return new FileDto
            {
                Id = file.Id,
                Name = file.Name,
                Size = file.Size.Bytes,
                Extension = file.Extension.Value,
                ContentType = file.ContentType,
                Status = file.Status,
                CaseId = file.CaseId,
                Tags = file.FileTags.Select(fileTag => fileTag.TagId).ToArray(),
                CreatedAt = file.CreatedAt,
                ModifiedAt = file.ModifiedAt
            };
        }
    }
}
