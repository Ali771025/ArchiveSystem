using ArchiveSystem.Application.Features.Classifications.Models;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Domain;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Classifications.Queries.GetClassificationTree
{
    public sealed class GetClassificationTreeQueryHandler
        : IRequestHandler<GetClassificationTreeQuery, IReadOnlyList<ClassificationTreeNodeDto>>
    {
        private const string ArchivedCasesTitle = "بایگانی راکد";
        private readonly IClassificationRepository _classificationRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public GetClassificationTreeQueryHandler(
            IClassificationRepository classificationRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _authorizer = authorizer;
        }

        public async Task<IReadOnlyList<ClassificationTreeNodeDto>> Handle(
            GetClassificationTreeQuery request,
            CancellationToken cancellationToken)
        {
            var allNodes = await _classificationRepository.GetAllAsync(cancellationToken);
            var allowedSet = await _authorizer.GetAccessibleSetAsync(
                allNodes,
                ClassificationPermissionMask.Read,
                cancellationToken);
            var dossierCounts = await _classificationRepository.GetDossierCountsByClassificationAsync(cancellationToken);

            var activeNodes = allNodes
                .Where(node => node.IsActive || IsArchivedCasesSystemNode(node))
                .ToArray();
            var activeNodesById = activeNodes.ToDictionary(node => node.Id);
            var visibleSet = new HashSet<long>(allowedSet);
            foreach (var allowedId in allowedSet)
            {
                var currentId = allowedId;
                while (activeNodesById.TryGetValue(currentId, out var currentNode) && currentNode.ParentId.HasValue)
                {
                    if (!visibleSet.Add(currentNode.ParentId.Value))
                    {
                        break;
                    }

                    currentId = currentNode.ParentId.Value;
                }
            }

            var childrenByParentId = activeNodes
                .Where(node => node.ParentId.HasValue)
                .GroupBy(node => node.ParentId!.Value)
                .ToDictionary(group => group.Key, group => group.ToArray());

            ClassificationTreeNodeDto? BuildAuthorizedBranch(Classification node)
            {
                var isArchivedCasesSystemNode = IsArchivedCasesSystemNode(node);
                if (!isArchivedCasesSystemNode && !visibleSet.Contains(node.Id))
                {
                    return null;
                }

                var dto = new ClassificationTreeNodeDto
                {
                    Id = node.Id,
                    Title = node.Title,
                    ParentId = isArchivedCasesSystemNode ? null : node.ParentId,
                    Path = isArchivedCasesSystemNode ? "/" : node.Path,
                    Level = isArchivedCasesSystemNode ? 0 : node.Level,
                    IsActive = node.IsActive,
                    DossierCount = (isArchivedCasesSystemNode || allowedSet.Contains(node.Id)) &&
                                   dossierCounts.TryGetValue(node.Id, out var count)
                        ? count
                        : 0,
                    SystemKey = isArchivedCasesSystemNode ? SystemClassificationKeys.ArchivedCases : node.SystemKey,
                    Children = new List<ClassificationTreeNodeDto>()
                };

                if (childrenByParentId.TryGetValue(node.Id, out var children))
                {
                    foreach (var child in children)
                    {
                        var authorizedChild = BuildAuthorizedBranch(child);
                        if (authorizedChild is not null)
                        {
                            dto.Children.Add(authorizedChild);
                        }
                    }
                }

                return dto;
            }

            var roots = activeNodes
                .Where(node => !node.ParentId.HasValue || IsArchivedCasesSystemNode(node))
                .Select(BuildAuthorizedBranch)
                .Where(node => node is not null)
                .Cast<ClassificationTreeNodeDto>()
                .ToList();

            SortTree(roots);
            return roots;
        }

        private static void SortTree(List<ClassificationTreeNodeDto> roots)
        {
            roots.Sort((left, right) => string.Compare(left.Title, right.Title, StringComparison.OrdinalIgnoreCase));
            foreach (var root in roots)
            {
                SortTree(root.Children);
            }
        }

        private static bool IsArchivedCasesSystemNode(Classification node)
        {
            return node.SystemKey == SystemClassificationKeys.ArchivedCases ||
                   string.Equals(node.Title.Trim(), ArchivedCasesTitle, StringComparison.Ordinal);
        }
    }
}
