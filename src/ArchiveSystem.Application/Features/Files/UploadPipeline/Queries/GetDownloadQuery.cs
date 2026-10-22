using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public class GetDownloadQuery : IRequest<DownloadFileResult?>
    {
        public long FileId { get; init; }
        public long? VersionId { get; init; }
    }

    public class DownloadFileResult
    {
        public Stream Content { get; init; } = Stream.Null;
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = "application/octet-stream";
        public long FileId { get; init; }
        public long VersionId { get; init; }
    }
}
