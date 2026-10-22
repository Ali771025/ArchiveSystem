using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.GetMovableClassificationIds
{
    public sealed class GetMovableClassificationIdsQueryHandler
        : IRequestHandler<GetMovableClassificationIdsQuery, IReadOnlyList<long>>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public GetMovableClassificationIdsQueryHandler(
            IClassificationRepository classificationRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _authorizer = authorizer;
        }

        public async Task<IReadOnlyList<long>> Handle(
            GetMovableClassificationIdsQuery request,
            CancellationToken cancellationToken)
        {
            var allNodes = await _classificationRepository.GetAllAsync(cancellationToken);
            var allowedSet = await _authorizer.GetAccessibleSetAsync(
                allNodes,
                ClassificationPermissionMask.Move,
                cancellationToken);
            var parentIds = allNodes
                .Where(node => node.ParentId.HasValue)
                .Select(node => node.ParentId!.Value)
                .ToHashSet();

            return allNodes
                .Where(node => node.IsActive && allowedSet.Contains(node.Id) && !parentIds.Contains(node.Id))
                .Select(node => node.Id)
                .OrderBy(id => id)
                .ToArray();
        }
    }
}
