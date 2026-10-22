using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Commands.CreateRole
{
    public sealed record CreateRoleCommand(string Name, bool IsSystem = false) : IRequest<RoleDto>;
}
