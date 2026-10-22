using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class InitiateUploadSessionCommand : IRequest<InitiateUploadSessionResponse>
    {
        public long? FileId { get; init; }
        public long? CaseId { get; init; }
        public long? ClassificationId { get; init; }
        public string OriginalFileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public long ExpectedSizeBytes { get; init; }
        public string ExpectedSha256 { get; init; } = string.Empty;
        public string? TenantId { get; init; }
    }

    public class InitiateUploadSessionResponse
    {
        public Guid SessionId { get; init; }
        public long FileId { get; init; }
        public int PartSizeBytes { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
    }
}
