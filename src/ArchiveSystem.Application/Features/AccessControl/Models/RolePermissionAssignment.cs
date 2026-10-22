namespace ArchiveSystem.Application.Features.AccessControl.Models
{
    public sealed record RolePermissionAssignment(long RoleId, IReadOnlyCollection<long> PermissionIds);
}
