using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveSystem.Application.Common.Interfaces.Security;

public interface IIdentityManagementService
{
    Task<IReadOnlyCollection<string>> GetRolePermissionClaimsAsync(string roleName, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetUserPermissionClaimsAsync(long userId, CancellationToken cancellationToken = default);

    Task<bool> HasUserPermissionAsync(long userId, string permissionKey, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> UpdateRolePermissionClaimsAsync(
        string roleName,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> UpdateUserRolesAsync(
        long userId,
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken = default);
}
