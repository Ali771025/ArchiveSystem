using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class FinalizeUploadCommand : IRequest<FinalizeUploadResponse>
    {
        public Guid SessionId { get; init; }
    }

    public class FinalizeUploadResponse
    {
        public long FileId { get; init; }
        public long VersionId { get; init; }
        public int VersionNumber { get; init; }
        public string Sha256Checksum { get; init; } = string.Empty;
        public long SizeBytes { get; init; }
        public string StoragePath { get; init; } = string.Empty;
        public bool AlreadyFinalized { get; init; }
    }
}
