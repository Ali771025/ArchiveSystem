// src/ArchiveSystem.Application/Features/Files/Commands/DeleteFile/DeleteFileCommand.cs
using MediatR;

namespace ArchiveSystem.Application.Features.Files.Commands.DeleteFile
{
    public class DeleteFileCommand : IRequest<Unit>
    {
        public long FileId { get; set; }

        public DeleteFileCommand(long fileId)
        {
            FileId = fileId;
        }
    }
}