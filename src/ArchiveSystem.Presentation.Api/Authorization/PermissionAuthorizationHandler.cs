using System.Security.Claims;
using ArchiveSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace ArchiveSystem.Presentation.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly RoleManager<IdentityRole<long>> _roleManager;

    public PermissionAuthorizationHandler(RoleManager<IdentityRole<long>> roleManager)
    {
        _roleManager = roleManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (context.User.IsInRole("Admin") || context.User.IsInRole("Superuser"))
        {
            context.Succeed(requirement);
            return;
        }

        if (context.User.HasClaim(
                claim => string.Equals(claim.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase)
                         && string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }

        var roles = context.User
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var roleClaims = await _roleManager.GetClaimsAsync(role);
            var hasPermissionClaim = roleClaims.Any(claim =>
                string.Equals(claim.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

            if (hasPermissionClaim)
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
