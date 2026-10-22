using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.GetMovableClassificationIds
{
    public sealed record GetMovableClassificationIdsQuery : IRequest<IReadOnlyList<long>>;
}
