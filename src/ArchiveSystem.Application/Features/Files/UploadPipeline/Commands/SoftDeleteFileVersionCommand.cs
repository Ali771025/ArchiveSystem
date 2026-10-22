using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class SoftDeleteFileVersionCommand : IRequest<Unit>
    {
        public long FileId { get; init; }
        public long VersionId { get; init; }
    }
}
