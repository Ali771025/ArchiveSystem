using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.AccessControl.Models
{
    public sealed record SubjectPermissionRecord(
        AccessSubjectType SubjectType,
        long SubjectId,
        long ResourceId,
        int PermissionMask,
        bool IsExplicit,
        byte InheritMode);
}
