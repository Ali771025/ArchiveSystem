using ArchiveSystem.Application.Features.Classifications.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.GetClassificationTree
{
    public sealed record GetClassificationTreeQuery : IRequest<IReadOnlyList<ClassificationTreeNodeDto>>;
}
