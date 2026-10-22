using ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetClassificationAcl
{
    public sealed class GetClassificationAclQueryHandler : IRequestHandler<GetClassificationAclQuery, IReadOnlyList<ClassificationAclEntryDto>>
    {
        private readonly IClassificationAclRepository _classificationAclRepository;

        public GetClassificationAclQueryHandler(IClassificationAclRepository classificationAclRepository)
        {
            _classificationAclRepository = classificationAclRepository;
        }

        public async Task<IReadOnlyList<ClassificationAclEntryDto>> Handle(GetClassificationAclQuery request, CancellationToken cancellationToken)
        {
            var entries = await _classificationAclRepository.GetByClassificationIdAsync(request.ClassificationId, cancellationToken);

            return entries
                .Select(x => new ClassificationAclEntryDto(
                    x.Id,
                    x.SubjectType,
                    x.SubjectId,
                    x.ClassificationId,
                    x.PermissionMask,
                    x.IsExplicit,
                    x.InheritMode))
                .ToArray();
        }
    }
}
