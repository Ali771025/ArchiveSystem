using MediatR;

namespace ArchiveSystem.Application.Features.Files.Queries.GetAssignableTags
{
    public sealed record GetAssignableTagsQuery : IRequest<IReadOnlyList<AssignableTagDto>>;

    public sealed record AssignableTagDto(long Id, string Name, string? Description);
}
