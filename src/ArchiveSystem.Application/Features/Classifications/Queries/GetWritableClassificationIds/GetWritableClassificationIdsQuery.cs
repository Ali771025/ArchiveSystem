using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.GetWritableClassificationIds
{
    public sealed record GetWritableClassificationIdsQuery : IRequest<IReadOnlyList<long>>;
}
