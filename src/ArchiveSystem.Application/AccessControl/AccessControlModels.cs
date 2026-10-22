using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.AccessControl
{
    public sealed record ClassificationNode(long Id, long? ParentId);

    public sealed record FileAccessDescriptor(long FileId, long? ClassificationId, IReadOnlyCollection<long> TagIds);

    public sealed record SubjectPermissionRecord(
        AccessSubjectType SubjectType,
        long SubjectId,
        long ResourceId,
        int PermissionMask,
        bool IsExplicit,
        byte InheritMode);

    public sealed record RolePermissionAssignment(long RoleId, IReadOnlyCollection<long> PermissionIds);
}
