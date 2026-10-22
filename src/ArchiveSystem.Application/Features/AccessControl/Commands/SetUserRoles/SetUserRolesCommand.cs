using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.SetUserRoles
{
    public sealed record SetUserRolesCommand(long UserId, IReadOnlyCollection<long> RoleIds) : IRequest<Unit>;
}
