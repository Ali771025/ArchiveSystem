using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public sealed class GetFileThumbnailQuery : IRequest<FileContentResult?>
    {
        public long FileId { get; init; }
        public long? VersionId { get; init; }
    }

    public sealed class FileContentResult
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string ContentType { get; init; } = "application/octet-stream";
    }
}
