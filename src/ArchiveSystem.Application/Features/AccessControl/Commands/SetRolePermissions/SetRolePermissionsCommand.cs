using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.SetRolePermissions
{
    public sealed record SetRolePermissionsCommand(long RoleId, IReadOnlyCollection<long> PermissionIds) : IRequest<Unit>;
}
