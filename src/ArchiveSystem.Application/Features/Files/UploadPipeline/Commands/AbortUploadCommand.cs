using MediatR;

namespace ArchiveSystem.Application.Features.Files.UploadPipeline.Commands
{
    public class AbortUploadCommand : IRequest<Unit>
    {
        public Guid SessionId { get; init; }
        public string? Reason { get; init; }
    }
}
