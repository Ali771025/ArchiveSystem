// src/ArchiveSystem.Application/Features/Files/Queries/GetFileById/GetFileByIdQuery.cs
using MediatR;
using ArchiveSystem.Application.Common.Models;

namespace ArchiveSystem.Application.Features.Files.Queries.GetFileById
{
    public class GetFileByIdQuery : IRequest<FileDto?>
    {
        public long FileId { get; set; }

        public GetFileByIdQuery(long fileId)
        {
            FileId = fileId;
        }
    }
}