using ArchiveSystem.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ArchiveSystem.Presentation.Api.Authorization;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallbackProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!TryParsePermissionPolicy(policyName, out var permission))
        {
            return _fallbackProvider.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public static string BuildPolicyName(string permission) => $"{PermissionPrefix}{permission}";

    public static bool TryParsePermissionPolicy(string policyName, out string permission)
    {
        if (policyName.StartsWith(PermissionPrefix, StringComparison.OrdinalIgnoreCase))
        {
            permission = policyName[PermissionPrefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(permission);
        }

        permission = string.Empty;
        return false;
    }

    public const string PermissionPrefix = "Permission:";
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission
    {
        get
        {
            var policy = Policy ?? string.Empty;
            return policy.StartsWith(PermissionPolicyProvider.PermissionPrefix, StringComparison.OrdinalIgnoreCase)
                ? policy[PermissionPolicyProvider.PermissionPrefix.Length..]
                : policy;
        }
        set => Policy = PermissionPolicyProvider.BuildPolicyName(value);
    }
}
