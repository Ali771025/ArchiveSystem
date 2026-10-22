using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public class ListVersionsQuery : IRequest<IReadOnlyList<FileVersionDto>>
    {
        public long FileId { get; init; }
    }

    public class FileVersionDto
    {
        public long VersionId { get; init; }
        public int VersionNumber { get; init; }
        public long Size { get; init; }
        public string ContentType { get; init; } = string.Empty;
        public string Sha256 { get; init; } = string.Empty;
        public string StoragePath { get; init; } = string.Empty;
        public string FileName { get; init; } = string.Empty;
        public bool IsDeleted { get; init; }
        public int ThumbnailStatus { get; init; }
        public int OcrStatus { get; init; }
        public string? OcrError { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? DeletedAt { get; init; }
    }
}
