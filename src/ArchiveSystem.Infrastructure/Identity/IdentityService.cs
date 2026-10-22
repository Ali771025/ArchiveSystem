using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ArchiveSystem.Infrastructure.Logging;

namespace ArchiveSystem.Infrastructure.Identity
{
    public sealed class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<long>> _roleManager;
        private readonly IOptions<JwtTokenOptions> _jwtOptions;
        private readonly ILogger<IdentityService> _logger;
        private readonly ISecurityEventLogger _securityEventLogger;
        private readonly IDistributedCache _sessionCache;
        private const string SecurityStampClaimType = "security_stamp";

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<long>> roleManager,
            IOptions<JwtTokenOptions> jwtOptions,
            ILogger<IdentityService> logger,
            ISecurityEventLogger securityEventLogger,
            IDistributedCache sessionCache)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtOptions = jwtOptions;
            _logger = logger;
            _securityEventLogger = securityEventLogger;
            _sessionCache = sessionCache;
        }

        public async Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await _userManager.FindByNameAsync(username.Trim());
            return user is not null;
        }

        public async Task<IdentityUserDto?> FindByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedInput = usernameOrEmail.Trim();
            if (string.IsNullOrWhiteSpace(normalizedInput))
            {
                return null;
            }

            var user = await _userManager.FindByNameAsync(normalizedInput);
            if (user is null)
            {
                user = await _userManager.FindByEmailAsync(normalizedInput);
            }

            return user is null ? null : Map(user);
        }

        public async Task<IdentityUserDto?> FindByIdAsync(long userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user is null ? null : Map(user);
        }

        public async Task<IdentityOperationResult> CreateUserAsync(
            string username,
            string password,
            IReadOnlyCollection<string> initialRoles,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedUsername = username.Trim();
            if (string.IsNullOrWhiteSpace(normalizedUsername))
            {
                return IdentityOperationResult.Failure("Username is required.");
            }

            var rolesToAssign = initialRoles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var role in rolesToAssign)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<long>
                    {
                        Name = role,
                        NormalizedName = role.ToUpperInvariant()
                    });

                    if (!createRoleResult.Succeeded)
                    {
                        return new IdentityOperationResult(false, createRoleResult.Errors.Select(e => e.Description).ToArray());
                    }
                }
            }

            var user = new ApplicationUser
            {
                UserName = normalizedUsername,
                Email = BuildSafeEmail(normalizedUsername),
                FirstName = normalizedUsername,
                LastName = "User",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                LockoutEnabled = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                return new IdentityOperationResult(false, createResult.Errors.Select(e => e.Description).ToArray());
            }

            if (rolesToAssign.Length > 0)
            {
                var addRolesResult = await _userManager.AddToRolesAsync(user, rolesToAssign);
                if (!addRolesResult.Succeeded)
                {
                    return new IdentityOperationResult(false, addRolesResult.Errors.Select(e => e.Description).ToArray());
                }
            }

            return IdentityOperationResult.Success();
        }

        public async Task<bool> ValidatePasswordAsync(long userId, string password, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null || !user.IsActive)
            {
                return false;
            }

            var valid = await _userManager.CheckPasswordAsync(user, password);
            if (!valid)
            {
                return false;
            }

            user.LastLoginAt = DateTime.UtcNow;
            user.ModifiedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            return true;
        }

        public async Task<string> CreateAccessTokenAsync(IdentityUserDto user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var settings = _jwtOptions.Value;
            if (string.IsNullOrWhiteSpace(settings.Secret) || settings.Secret.Length < 32)
            {
                throw new InvalidOperationException("JWT secret must be configured with at least 32 characters.");
            }

            var trackedUser = await _userManager.FindByIdAsync(user.Id.ToString())
                ?? throw new InvalidOperationException("User not found while creating access token.");
            await _userManager.UpdateSecurityStampAsync(trackedUser);
            var securityStamp = await _userManager.GetSecurityStampAsync(trackedUser);
            await StoreActiveSessionAsync(trackedUser.Id, securityStamp, cancellationToken);

            var roles = await GetUserRolesAsync(user.Id, cancellationToken);
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new("first_name", user.FirstName),
                new("last_name", user.LastName),
                new(SecurityStampClaimType, securityStamp),
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Email));
            }

            if (!string.IsNullOrWhiteSpace(user.PersonnelCode))
            {
                claims.Add(new Claim("personnel_code", user.PersonnelCode));
            }

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role is null)
                {
                    continue;
                }

                var roleClaims = await _roleManager.GetClaimsAsync(role);
                foreach (var claim in roleClaims.Where(claim =>
                             string.Equals(claim.Type, Permissions.ClaimType, StringComparison.OrdinalIgnoreCase)))
                {
                    permissions.Add(claim.Value);
                }
            }

            foreach (var permission in permissions)
            {
                claims.Add(new Claim(Permissions.ClaimType, permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = settings.ExpirationMinutes <= 0 ? 60 : settings.ExpirationMinutes;

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiration),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task StoreActiveSessionAsync(long userId, string securityStamp, CancellationToken cancellationToken)
        {
            var idleTimeoutMinutes = _jwtOptions.Value.IdleTimeoutMinutes <= 0 ? 20 : _jwtOptions.Value.IdleTimeoutMinutes;
            await _sessionCache.SetStringAsync(
                BuildSessionCacheKey(userId, securityStamp),
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(idleTimeoutMinutes)
                },
                cancellationToken);
        }

        private static string BuildSessionCacheKey(long userId, string securityStamp)
        {
            return $"auth:session:{userId}:{securityStamp}";
        }

        public async Task<IReadOnlyCollection<string>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return Array.Empty<string>();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return roles.ToArray();
        }

        public async Task<IReadOnlyCollection<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _roleManager.Roles
                .OrderBy(role => role.Name)
                .Select(role => role.Name!)
                .ToArrayAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<IdentityUserWithRolesDto>> GetUsersWithRolesAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userManager.Users
                .OrderBy(user => user.UserName)
                .ToListAsync(cancellationToken);

            var result = new List<IdentityUserWithRolesDto>(users.Count);
            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new IdentityUserWithRolesDto(
                    user.Id,
                    user.UserName ?? string.Empty,
                    user.Email ?? string.Empty,
                    user.IsActive,
                    roles.ToArray()));
            }

            return result;
        }

        public async Task<IdentityOperationResult> SetUserRolesAsync(long userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return IdentityOperationResult.Failure($"User with ID {userId} was not found.");
            }

            var normalizedRoles = roles
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var unknownRoles = new List<string>();
            foreach (var role in normalizedRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    unknownRoles.Add(role);
                }
            }

            if (unknownRoles.Count > 0)
            {
                return IdentityOperationResult.Failure($"Unknown role(s): {string.Join(", ", unknownRoles)}.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return new IdentityOperationResult(false, removeResult.Errors.Select(e => e.Description).ToArray());
            }

            if (normalizedRoles.Length > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, normalizedRoles);
                if (!addResult.Succeeded)
                {
                    return new IdentityOperationResult(false, addResult.Errors.Select(e => e.Description).ToArray());
                }
            }

            user.ModifiedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            return IdentityOperationResult.Success();
        }

        public async Task<IdentityOperationResult> StartForgotPasswordAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await FindByUsernameOrEmailAsync(usernameOrEmail, cancellationToken);
            if (user is null)
            {
                return IdentityOperationResult.Success();
            }

            var trackedUser = await _userManager.FindByIdAsync(user.Id.ToString());
            if (trackedUser is null)
            {
                return IdentityOperationResult.Success();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(trackedUser);
            trackedUser.LastPasswordResetRequestedAt = DateTime.UtcNow;
            trackedUser.ModifiedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(trackedUser);
            if (!updateResult.Succeeded)
            {
                return new IdentityOperationResult(false, updateResult.Errors.Select(e => e.Description).ToArray());
            }

            _logger.LogInformation("Password reset requested for user {UserId} ({UserName}).", trackedUser.Id, trackedUser.UserName);
            _logger.LogDebug("Password reset token generated for user {UserId}. Token length: {Length}", trackedUser.Id, token.Length);
            await _securityEventLogger.LogAsync(
                eventType: "PasswordChanged",
                description: $"Password reset flow initiated for user '{trackedUser.UserName}'.",
                actorUserId: trackedUser.Id.ToString(),
                targetUserId: trackedUser.Id.ToString(),
                metadata: new { trackedUser.UserName, Action = "ResetRequested" },
                cancellationToken: cancellationToken);

            return IdentityOperationResult.Success();
        }

        private static IdentityUserDto Map(ApplicationUser user)
        {
            return new IdentityUserDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.IsActive,
                user.FirstName,
                user.LastName,
                user.PersonnelCode);
        }

        private static string BuildSafeEmail(string username)
        {
            if (username.Contains('@'))
            {
                return username;
            }

            var localPart = username
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", ".")
                .Replace("..", ".");

            return $"{localPart}@archive.local";
        }
    }
}
