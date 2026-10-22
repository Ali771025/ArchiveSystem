using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Commands.MoveClassification
{
    public sealed record MoveClassificationCommand(long Id, long? NewParentId) : IRequest<Unit>;
}
