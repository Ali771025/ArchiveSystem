using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Commands.DeleteClassification
{
    public sealed record DeleteClassificationCommand(long Id) : IRequest<Unit>;
}
