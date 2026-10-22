using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetUserRoles
{
    public sealed record GetUserRolesQuery(long UserId) : IRequest<IReadOnlyList<RoleDto>>;
}
