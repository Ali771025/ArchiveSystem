using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Queries
{
    public sealed class GetFileOcrQuery : IRequest<string?>
    {
        public long FileId { get; init; }
        public long? VersionId { get; init; }
    }
}
