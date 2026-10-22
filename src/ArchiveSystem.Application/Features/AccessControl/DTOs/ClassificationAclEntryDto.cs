using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.AccessControl.DTOs
{
    public sealed record ClassificationAclEntryDto(
        long Id,
        AccessSubjectType SubjectType,
        long SubjectId,
        long ClassificationId,
        ClassificationPermissionMask PermissionMask,
        bool IsExplicit,
        ClassificationAclInheritMode InheritMode,
        bool Cascade = false);
}
