using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRoleById
{
    public sealed record GetRoleByIdQuery(long RoleId) : IRequest<RoleDto?>;
}
