using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetTagAcl
{
    public sealed class GetTagAclQueryHandler : IRequestHandler<GetTagAclQuery, IReadOnlyList<TagAclEntryDto>>
    {
        private readonly ITagAclRepository _tagAclRepository;

        public GetTagAclQueryHandler(ITagAclRepository tagAclRepository)
        {
            _tagAclRepository = tagAclRepository;
        }

        public async Task<IReadOnlyList<TagAclEntryDto>> Handle(GetTagAclQuery request, CancellationToken cancellationToken)
        {
            var entries = await _tagAclRepository.GetByTagIdAsync(request.TagId, cancellationToken);

            return entries
                .Select(x => new TagAclEntryDto(
                    x.Id,
                    x.SubjectType,
                    x.SubjectId,
                    x.TagId,
                    x.PermissionMask))
                .ToArray();
        }
    }
}
