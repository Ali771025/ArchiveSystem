using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.GetWritableClassificationIds
{
    public sealed class GetWritableClassificationIdsQueryHandler
        : IRequestHandler<GetWritableClassificationIdsQuery, IReadOnlyList<long>>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public GetWritableClassificationIdsQueryHandler(
            IClassificationRepository classificationRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _authorizer = authorizer;
        }

        public async Task<IReadOnlyList<long>> Handle(
            GetWritableClassificationIdsQuery request,
            CancellationToken cancellationToken)
        {
            var allNodes = await _classificationRepository.GetAllAsync(cancellationToken);
            var allowedSet = await _authorizer.GetAccessibleSetAsync(
                allNodes,
                ClassificationPermissionMask.Write,
                cancellationToken);
            var parentIds = allNodes
                .Where(node => node.ParentId.HasValue)
                .Select(node => node.ParentId!.Value)
                .ToHashSet();

            return allNodes
                .Where(x => x.IsActive && allowedSet.Contains(x.Id) && !parentIds.Contains(x.Id))
                .Select(x => x.Id)
                .OrderBy(id => id)
                .ToArray();
        }
    }
}
