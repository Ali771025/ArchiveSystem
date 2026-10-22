using MediatR;
using TagRepository = ArchiveSystem.Application.AccessControl.ITagRepository;

namespace ArchiveSystem.Application.Features.Files.Queries.GetAssignableTags
{
    public sealed class GetAssignableTagsQueryHandler
        : IRequestHandler<GetAssignableTagsQuery, IReadOnlyList<AssignableTagDto>>
    {
        private readonly TagRepository _tagRepository;

        public GetAssignableTagsQueryHandler(TagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public async Task<IReadOnlyList<AssignableTagDto>> Handle(
            GetAssignableTagsQuery request,
            CancellationToken cancellationToken)
        {
            var tags = await _tagRepository.GetAllAsync(cancellationToken);
            return tags
                .OrderBy(tag => tag.Name)
                .Select(tag => new AssignableTagDto(tag.Id, tag.Name, tag.Description))
                .ToArray();
        }
    }
}
