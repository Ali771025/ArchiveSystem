using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.UpdateRole
{
    public sealed record UpdateRoleCommand(long RoleId, string Name) : IRequest<RoleDto>;
}
