// src/ArchiveSystem.Application/Common/Models/SearchResultDto.cs
namespace ArchiveSystem.Application.Common.Models
{
    public class SearchResultDto
    {
        public long FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string HighlightedContent { get; set; } = string.Empty;
        public float Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }
}