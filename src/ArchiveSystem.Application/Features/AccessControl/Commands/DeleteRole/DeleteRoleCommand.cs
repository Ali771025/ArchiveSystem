using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.DeleteRole
{
    public sealed record DeleteRoleCommand(long RoleId) : IRequest<Unit>;
}
