using ArchiveSystem.Application.Features.AccessControl.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetPermissions
{
    public sealed record GetPermissionsQuery : IRequest<IReadOnlyList<PermissionDto>>;
}
