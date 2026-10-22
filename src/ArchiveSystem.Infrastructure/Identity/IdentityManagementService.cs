using System.Security.Claims;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Interfaces.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Identity;

public sealed class IdentityManagementService : IIdentityManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<long>> _roleManager;

    public IdentityManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<long>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyCollection<string>> GetRolePermissionClaimsAsync(string roleName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedRoleName = NormalizeRoleName(roleName);
        if (string.IsNullOrWhiteSpace(normalizedRoleName))
        {
            return Array.Empty<string>();
        }

        var role = await _roleManager.FindByNameAsync(normalizedRoleName);
        if (role is null)
        {
            return Array.Empty<string>();
        }

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims
            .Where(claim => string.Equals(claim.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<string>> GetUserPermissionClaimsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims.Where(claim =>
                         string.Equals(claim.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase)))
            {
                permissions.Add(claim.Value);
            }
        }

        return permissions.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<bool> HasUserPermissionAsync(
        long userId,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(permissionKey))
        {
            return false;
        }

        var permissions = await GetUserPermissionClaimsAsync(userId, cancellationToken);
        return permissions.Contains(permissionKey.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IdentityOperationResult> UpdateRolePermissionClaimsAsync(
        string roleName,
        IReadOnlyCollection<string> permissionKeys,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedRoleName = NormalizeRoleName(roleName);
        if (string.IsNullOrWhiteSpace(normalizedRoleName))
        {
            return IdentityOperationResult.Failure("Role name is required.");
        }

        var role = await _roleManager.FindByNameAsync(normalizedRoleName);
        if (role is null)
        {
            return IdentityOperationResult.Failure($"Role '{normalizedRoleName}' was not found.");
        }

        var normalizedPermissions = NormalizeDistinct(permissionKeys);
        var unknownPermissions = normalizedPermissions
            .Where(permission => !Permissions.IsDefined(permission))
            .OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (unknownPermissions.Length > 0)
        {
            return IdentityOperationResult.Failure(
                $"Unknown permission(s): {string.Join(", ", unknownPermissions)}.");
        }

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var existingPermissionClaims = existingClaims
            .Where(claim => string.Equals(claim.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var claim in existingPermissionClaims)
        {
            var removeResult = await _roleManager.RemoveClaimAsync(role, claim);
            if (!removeResult.Succeeded)
            {
                return IdentityOperationResult.Failure(removeResult.Errors.Select(x => x.Description).ToArray());
            }
        }

        foreach (var permission in normalizedPermissions)
        {
            var addResult = await _roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
            if (!addResult.Succeeded)
            {
                return IdentityOperationResult.Failure(addResult.Errors.Select(x => x.Description).ToArray());
            }
        }

        return IdentityOperationResult.Success();
    }

    public async Task<IReadOnlyCollection<string>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<IdentityOperationResult> UpdateUserRolesAsync(
        long userId,
        IReadOnlyCollection<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return IdentityOperationResult.Failure($"User with ID {userId} was not found.");
        }

        var normalizedRoleNames = NormalizeDistinct(roleNames);
        var unknownRoles = new List<string>();

        foreach (var roleName in normalizedRoleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                unknownRoles.Add(roleName);
            }
        }

        if (unknownRoles.Count > 0)
        {
            return IdentityOperationResult.Failure(
                $"Unknown role(s): {string.Join(", ", unknownRoles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))}.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles
            .Except(normalizedRoleNames, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rolesToAdd = normalizedRoleNames
            .Except(currentRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return IdentityOperationResult.Failure(removeResult.Errors.Select(x => x.Description).ToArray());
            }
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return IdentityOperationResult.Failure(addResult.Errors.Select(x => x.Description).ToArray());
            }
        }

        user.ModifiedAt = DateTime.UtcNow;
        var updateUserResult = await _userManager.UpdateAsync(user);
        if (!updateUserResult.Succeeded)
        {
            return IdentityOperationResult.Failure(updateUserResult.Errors.Select(x => x.Description).ToArray());
        }

        return IdentityOperationResult.Success();
    }

    private static string NormalizeRoleName(string roleName) => roleName.Trim();

    private static string[] NormalizeDistinct(IReadOnlyCollection<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
