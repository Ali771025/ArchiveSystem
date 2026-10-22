using ArchiveSystem.Application.Features.Classifications.Models;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.SearchClassifications
{
    public sealed record SearchClassificationsQuery(string? Text) : IRequest<IReadOnlyList<ClassificationSearchNodeDto>>;
}
