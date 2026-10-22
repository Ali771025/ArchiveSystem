using System.Collections.Generic;

namespace ArchiveSystem.Application.Features.UserManagement.DTOs
{
    public sealed record AssignUserRolesRequestDto(long UserId, IReadOnlyCollection<string> Roles);

    public sealed record UserRolesDto(
        long UserId,
        string Username,
        string Email,
        bool IsActive,
        IReadOnlyCollection<string> Roles);
}
