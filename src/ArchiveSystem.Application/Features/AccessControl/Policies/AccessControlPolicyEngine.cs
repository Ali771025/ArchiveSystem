using ArchiveSystem.Application.Features.AccessControl.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.AccessControl.Policies
{
    public static class AccessControlPolicyEngine
    {
        public static IReadOnlyDictionary<long, ClassificationPermissionMask> ComputeEffectiveClassificationPermissions(
            IReadOnlyList<ClassificationNode> nodes,
            IReadOnlyList<ClassificationAclEntry> aclEntries)
        {
            if (nodes.Count == 0)
            {
                return new Dictionary<long, ClassificationPermissionMask>();
            }

            var subjects = aclEntries
                .Select(x => (x.SubjectType, x.SubjectId))
                .Distinct()
                .ToArray();

            var entriesBySubjectAndClassification = aclEntries
                .GroupBy(x => (x.SubjectType, x.SubjectId, x.ClassificationId))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var mergedMask = g.Aggregate(ClassificationPermissionMask.None, (acc, item) => acc | item.PermissionMask);
                        var inheritMode = g.OrderByDescending(x => x.Id).Select(x => x.InheritMode).FirstOrDefault();
                        return (Mask: mergedMask, InheritMode: inheritMode);
                    });

            var children = nodes
                .Where(x => x.ParentId.HasValue)
                .GroupBy(x => x.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());
            var nodeIds = nodes.Select(x => x.Id).ToHashSet();
            var roots = nodes
                .Where(x => !x.ParentId.HasValue || !nodeIds.Contains(x.ParentId.Value))
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            var effectiveBySubject = new Dictionary<(AccessSubjectType, long), Dictionary<long, ClassificationPermissionMask>>();

            foreach (var subject in subjects)
            {
                var subjectEffective = new Dictionary<long, ClassificationPermissionMask>();
                effectiveBySubject[subject] = subjectEffective;

                foreach (var rootId in roots)
                {
                    Traverse(rootId, subject, ClassificationPermissionMask.None);
                }

                foreach (var node in nodes)
                {
                    if (!subjectEffective.ContainsKey(node.Id))
                    {
                        Traverse(node.Id, subject, ClassificationPermissionMask.None);
                    }
                }

                void Traverse(long nodeId, (AccessSubjectType SubjectType, long SubjectId) currentSubject, ClassificationPermissionMask parentMask)
                {
                    if (subjectEffective.ContainsKey(nodeId))
                    {
                        return;
                    }

                    var key = (currentSubject.SubjectType, currentSubject.SubjectId, nodeId);
                    var hasExplicit = entriesBySubjectAndClassification.TryGetValue(key, out var explicitEntry);
                    var currentMask = !hasExplicit
                        ? parentMask
                        : explicitEntry.InheritMode == ClassificationAclInheritMode.Inherit
                            ? parentMask | explicitEntry.Mask
                            : explicitEntry.Mask;

                    subjectEffective[nodeId] = currentMask;

                    if (!children.TryGetValue(nodeId, out var childIds))
                    {
                        return;
                    }

                    foreach (var childId in childIds)
                    {
                        Traverse(childId, currentSubject, currentMask);
                    }
                }
            }

            var result = nodes.ToDictionary(x => x.Id, _ => ClassificationPermissionMask.None);
            foreach (var perSubject in effectiveBySubject.Values)
            {
                foreach (var kvp in perSubject)
                {
                    result[kvp.Key] |= kvp.Value;
                }
            }

            return result;
        }

        public static IReadOnlyDictionary<long, TagPermissionMask> ComputeEffectiveTagPermissions(
            IReadOnlyList<TagAclEntry> aclEntries)
        {
            return aclEntries
                .GroupBy(x => x.TagId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Aggregate(TagPermissionMask.None, (acc, item) => acc | item.PermissionMask));
        }

        public static bool HasAllTagPermissions(
            IReadOnlyCollection<long> fileTagIds,
            IReadOnlyCollection<long> allowedTagIds)
        {
            if (fileTagIds.Count == 0)
            {
                return true;
            }

            var allowed = allowedTagIds.Count > 0
                ? allowedTagIds.ToHashSet()
                : new HashSet<long>();

            return fileTagIds.All(allowed.Contains);
        }
    }
}
