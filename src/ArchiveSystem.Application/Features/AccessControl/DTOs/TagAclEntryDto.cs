using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.AccessControl.DTOs
{
    public sealed record TagAclEntryDto(
        long Id,
        AccessSubjectType SubjectType,
        long SubjectId,
        long TagId,
        TagPermissionMask PermissionMask);
}
