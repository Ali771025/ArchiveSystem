using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Features.UserManagement.DTOs;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;

namespace ArchiveSystem.Application.Features.UserManagement.Services
{
    public sealed class UserRoleManagementService : IUserRoleManagementService
    {
        private readonly IIdentityService _identityService;

        public UserRoleManagementService(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        public async Task<IReadOnlyCollection<UserRolesDto>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default)
        {
            var users = await _identityService.GetUsersWithRolesAsync(cancellationToken);
            return users
                .Select(user => new UserRolesDto(
                    user.Id,
                    user.Username,
                    user.Email,
                    user.IsActive,
                    user.Roles.OrderBy(x => x).ToArray()))
                .ToArray();
        }

        public async Task<IReadOnlyCollection<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _identityService.GetAvailableRolesAsync(cancellationToken);
            return roles.OrderBy(x => x).ToArray();
        }

        public async Task<UserRolesDto> AssignRolesAsync(AssignUserRolesRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _identityService.FindByIdAsync(request.UserId, cancellationToken);
            if (user is null)
            {
                throw new NotFoundException($"User with ID {request.UserId} was not found.");
            }

            var normalizedRoles = NormalizeRoles(request.Roles);
            var availableRoles = await _identityService.GetAvailableRolesAsync(cancellationToken);
            var unknownRoles = normalizedRoles
                .Where(role => !availableRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (unknownRoles.Length > 0)
            {
                throw new ValidationException($"Unknown roles: {string.Join(", ", unknownRoles)}.");
            }

            var result = await _identityService.SetUserRolesAsync(request.UserId, normalizedRoles, cancellationToken);
            if (!result.Succeeded)
            {
                throw new ConflictException(string.Join("; ", result.Errors));
            }

            var userRoles = await _identityService.GetUserRolesAsync(request.UserId, cancellationToken);
            return new UserRolesDto(
                user.Id,
                user.Username,
                user.Email,
                user.IsActive,
                userRoles.OrderBy(x => x).ToArray());
        }

        private static IReadOnlyCollection<string> NormalizeRoles(IEnumerable<string> roles)
        {
            return roles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
