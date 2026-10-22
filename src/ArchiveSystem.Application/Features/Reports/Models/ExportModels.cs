using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.Reports.Models
{
    public sealed class ExportJobDto
    {
        public Guid Id { get; set; }
        public long CreatedByUserId { get; set; }
        public ExportJobType Type { get; set; }
        public ExportJobStatus Status { get; set; }
        public int? ProgressPercent { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public sealed class ActivityExcelExportRequest
    {
        public ActivityReportFilter Filter { get; set; } = new();
    }

    public sealed class FilesZipExportRequest
    {
        public string? Term { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public long? ClassificationId { get; set; }
        public long? CaseId { get; set; }
        public long? FileId { get; set; }
        public long? TagId { get; set; }
    }

    public sealed class FileExportItemDto
    {
        public long FileId { get; set; }
        public long? CaseId { get; set; }
        public long? ClassificationId { get; set; }
        public string CaseNumber { get; set; } = string.Empty;
        public string CaseTitle { get; set; } = string.Empty;
        public string CaseOwners { get; set; } = string.Empty;
        public string CaseLocation { get; set; } = string.Empty;
        public string? CasePostalCode { get; set; }
        public long VersionId { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }
        public string ObjectKey { get; set; } = string.Empty;
        public IReadOnlyCollection<long> TagIds { get; set; } = Array.Empty<long>();
    }

    public sealed class ExportDownloadResult
    {
        public required Stream Content { get; init; }
        public required string ContentType { get; init; }
        public required string FileName { get; init; }
    }
}
