using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.AccessControl
{
    public sealed record RoleDto(long Id, string Name, bool IsSystem);

    public sealed record PermissionDto(long Id, string Key, string Description);

    public sealed record ClassificationAclEntryDto(
        long Id,
        AccessSubjectType SubjectType,
        long SubjectId,
        long ClassificationId,
        ClassificationPermissionMask PermissionMask,
        bool IsExplicit,
        ClassificationAclInheritMode InheritMode,
        bool Cascade = false);

    public sealed record TagAclEntryDto(
        long Id,
        AccessSubjectType SubjectType,
        long SubjectId,
        long TagId,
        TagPermissionMask PermissionMask);
}
