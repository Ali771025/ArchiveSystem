using MediatR;

namespace ArchiveSystem.Application.Features.AccessControl.Queries.GetRolePermissionIds
{
    public sealed record GetRolePermissionIdsQuery(long RoleId) : IRequest<IReadOnlyCollection<long>>;
}
