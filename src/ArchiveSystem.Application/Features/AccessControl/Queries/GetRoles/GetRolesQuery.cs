using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRoles
{
    public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleDto>>;
}
