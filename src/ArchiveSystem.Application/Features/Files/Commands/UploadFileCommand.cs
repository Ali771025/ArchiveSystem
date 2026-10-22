// src/ArchiveSystem.Application/Features/Files/Commands/UploadFile/UploadFileCommand.cs
using MediatR;
using ArchiveSystem.Application.Common.Models;

namespace ArchiveSystem.Application.Features.Files.Commands.UploadFile
{
    public class UploadFileCommand : IRequest<UploadFileResponse>
    {
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string TusId { get; set; } = string.Empty; // For tus upload tracking
        public long? CaseId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
    }

    public class UploadFileResponse
    {
        public long FileId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}