// src/ArchiveSystem.Application/Common/Models/FileDto.cs
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Common.Models
{
    public class FileDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Extension { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public FileStatus Status { get; set; }
        public long? CaseId { get; set; }
        public int? LatestVersionNumber { get; set; }
        public int ThumbnailStatus { get; set; }
        public int OcrStatus { get; set; }
        public string? OcrError { get; set; }
        public IReadOnlyCollection<long> Tags { get; set; } = Array.Empty<long>();
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
