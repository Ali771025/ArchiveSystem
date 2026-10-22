using ArchiveSystem.Application.Features.Classifications.Models;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.SearchClassifications
{
    public sealed class SearchClassificationsQueryHandler
        : IRequestHandler<SearchClassificationsQuery, IReadOnlyList<ClassificationSearchNodeDto>>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public SearchClassificationsQueryHandler(
            IClassificationRepository classificationRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _authorizer = authorizer;
        }

        public async Task<IReadOnlyList<ClassificationSearchNodeDto>> Handle(
            SearchClassificationsQuery request,
            CancellationToken cancellationToken)
        {
            var text = request.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<ClassificationSearchNodeDto>();
            }

            var allNodes = await _classificationRepository.GetAllAsync(cancellationToken);
            var allowedSet = await _authorizer.GetAccessibleSetAsync(
                allNodes,
                ClassificationPermissionMask.Read,
                cancellationToken);
            var visibleMap = allNodes
                .Where(x => x.IsActive && allowedSet.Contains(x.Id))
                .ToDictionary(x => x.Id);

            var matches = await _classificationRepository.SearchByTitleAsync(text, 100, cancellationToken);
            var dossierCounts = await _classificationRepository.GetDossierCountsByClassificationAsync(cancellationToken);

            return matches
                .Where(x => visibleMap.ContainsKey(x.Id))
                .Select(node => new ClassificationSearchNodeDto
                {
                    Id = node.Id,
                    Title = node.Title,
                    ParentId = node.ParentId,
                    DossierCount = dossierCounts.TryGetValue(node.Id, out var count) ? count : 0,
                    HierarchyPath = BuildTitlePath(node.Id, visibleMap)
                })
                .OrderBy(x => x.HierarchyPath)
                .ToList();
        }

        private static string BuildTitlePath(long nodeId, IReadOnlyDictionary<long, Classification> nodeMap)
        {
            var segments = new List<string>();
            var pointerId = nodeId;

            while (nodeMap.TryGetValue(pointerId, out var node))
            {
                segments.Add(node.Title);
                if (!node.ParentId.HasValue)
                {
                    break;
                }

                pointerId = node.ParentId.Value;
            }

            segments.Reverse();
            return string.Join(" > ", segments);
        }
    }
}
