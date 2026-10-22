using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.AccessControl.DTOs;
using ArchiveSystem.Application.Features.AccessControl.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Identity;

public sealed class IdentityRoleService : IIdentityRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<long>> _roleManager;

    public IdentityRoleService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<long>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToArrayAsync(cancellationToken);

        return roles
            .Select(role => new RoleDto(role.Id, role.Name ?? string.Empty, IsSystemRole(role.Name)))
            .ToArray();
    }

    public async Task<RoleDto?> GetRoleByIdAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        return role is null ? null : new RoleDto(role.Id, role.Name ?? string.Empty, IsSystemRole(role.Name));
    }

    public async Task<RoleDto> CreateRoleAsync(string name, bool isSystem, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedName = name.Trim();
        _ = isSystem;

        if (await _roleManager.RoleExistsAsync(normalizedName))
        {
            throw new ConflictException($"Role '{normalizedName}' already exists.");
        }

        var role = new IdentityRole<long>(normalizedName);
        var result = await _roleManager.CreateAsync(role);
        EnsureSucceeded(result);

        return new RoleDto(role.Id, role.Name ?? normalizedName, IsSystemRole(role.Name));
    }

    public async Task<RoleDto> UpdateRoleAsync(long roleId, string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            throw new NotFoundException($"Role with ID {roleId} was not found.");
        }

        if (IsSystemRole(role.Name))
        {
            throw new ConflictException("System roles cannot be renamed.");
        }

        var normalizedName = name.Trim();
        var existingRole = await _roleManager.FindByNameAsync(normalizedName);
        if (existingRole is not null && existingRole.Id != roleId)
        {
            throw new ConflictException($"Role '{normalizedName}' already exists.");
        }

        role.Name = normalizedName;
        var result = await _roleManager.UpdateAsync(role);
        EnsureSucceeded(result);

        return new RoleDto(role.Id, role.Name ?? normalizedName, IsSystemRole(role.Name));
    }

    public async Task DeleteRoleAsync(long roleId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            throw new NotFoundException($"Role with ID {roleId} was not found.");
        }

        if (IsSystemRole(role.Name))
        {
            throw new ConflictException("System roles cannot be deleted.");
        }

        var result = await _roleManager.DeleteAsync(role);
        EnsureSucceeded(result);
    }

    public async Task<IReadOnlyList<RoleDto>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Array.Empty<RoleDto>();
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
        {
            return Array.Empty<RoleDto>();
        }

        var roles = await _roleManager.Roles
            .AsNoTracking()
            .Where(role => role.Name != null && roleNames.Contains(role.Name))
            .OrderBy(role => role.Name)
            .ToArrayAsync(cancellationToken);

        return roles
            .Select(role => new RoleDto(role.Id, role.Name ?? string.Empty, IsSystemRole(role.Name)))
            .ToArray();
    }

    public async Task SetUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException($"User with ID {userId} was not found.");
        }

        var requestedRoleIds = roleIds.Where(id => id > 0).Distinct().ToArray();
        var roles = await _roleManager.Roles
            .Where(role => requestedRoleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);

        var missingRoleIds = requestedRoleIds.Except(roles.Select(role => role.Id)).OrderBy(id => id).ToArray();
        if (missingRoleIds.Length > 0)
        {
            throw new NotFoundException($"Role(s) with ID(s) {string.Join(", ", missingRoleIds)} were not found.");
        }

        var targetRoleNames = roles
            .Select(role => role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToArray();
        var currentRoleNames = await _userManager.GetRolesAsync(user);

        var toRemove = currentRoleNames.Except(targetRoleNames, StringComparer.OrdinalIgnoreCase).ToArray();
        var toAdd = targetRoleNames.Except(currentRoleNames, StringComparer.OrdinalIgnoreCase).ToArray();

        if (toRemove.Length > 0)
        {
            EnsureSucceeded(await _userManager.RemoveFromRolesAsync(user, toRemove));
        }

        if (toAdd.Length > 0)
        {
            EnsureSucceeded(await _userManager.AddToRolesAsync(user, toAdd));
        }
    }

    private static bool IsSystemRole(string? roleName)
    {
        return string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(roleName, "Viewer", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new ValidationException(string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }
}
