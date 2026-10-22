using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveSystem.Application.Common.Interfaces.Security
{
    public interface IIdentityService
    {
        Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
        Task<IdentityUserDto?> FindByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
        Task<IdentityUserDto?> FindByIdAsync(long userId, CancellationToken cancellationToken = default);
        Task<IdentityOperationResult> CreateUserAsync(
            string username,
            string password,
            IReadOnlyCollection<string> initialRoles,
            CancellationToken cancellationToken = default);
        Task<bool> ValidatePasswordAsync(long userId, string password, CancellationToken cancellationToken = default);
        Task<string> CreateAccessTokenAsync(IdentityUserDto user, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<string>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<IdentityUserWithRolesDto>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default);
        Task<IdentityOperationResult> SetUserRolesAsync(long userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);
        Task<IdentityOperationResult> StartForgotPasswordAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
    }

    public sealed record IdentityUserDto(
        long Id,
        string Username,
        string Email,
        bool IsActive,
        string FirstName = "",
        string LastName = "",
        string? PersonnelCode = null);

    public sealed record IdentityUserWithRolesDto(
        long Id,
        string Username,
        string Email,
        bool IsActive,
        IReadOnlyCollection<string> Roles);

    public sealed record IdentityOperationResult(bool Succeeded, IReadOnlyCollection<string> Errors)
    {
        public static IdentityOperationResult Success()
        {
            return new IdentityOperationResult(true, Array.Empty<string>());
        }

        public static IdentityOperationResult Failure(params string[] errors)
        {
            return new IdentityOperationResult(false, errors ?? Array.Empty<string>());
        }
    }
}
