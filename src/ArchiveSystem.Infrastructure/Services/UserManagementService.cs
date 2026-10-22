using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Application.Features.UserManagement.Interfaces;
using ArchiveSystem.Infrastructure.Identity;
using ArchiveSystem.Infrastructure.Logging;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Services
{
    public sealed class UserManagementService : IUserManagementService
    {
        private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "UserName",
            "Email",
            "FirstName",
            "LastName",
            "NationalId",
            "PersonnelCode",
            "CreatedAt",
            "LastModified",
            "IsActive"
        };

        private readonly ArchiveDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISecurityEventLogger _securityEventLogger;

        public UserManagementService(
            ArchiveDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            ISecurityEventLogger securityEventLogger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _securityEventLogger = securityEventLogger;
        }

        public async Task<PagedResponse<UserListItemDto>> GetUsersWithPaginationAsync(
            int pageNumber,
            int pageSize,
            string? sortBy,
            bool sortDescending,
            string? username,
            string? name,
            string? firstName,
            string? lastName,
            string? nationalId,
            string? columnFilterField,
            string? columnFilterOperator,
            string? columnFilterValue,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(username))
            {
                var normalized = username.Trim();
                query = query.Where(u => u.UserName != null && u.UserName.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var normalized = name.Trim();
                query = query.Where(u =>
                    (u.FirstName + " " + u.LastName).Contains(normalized) ||
                    (u.LastName + " " + u.FirstName).Contains(normalized) ||
                    u.FirstName.Contains(normalized) ||
                    u.LastName.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                var normalized = firstName.Trim();
                query = query.Where(u => u.FirstName.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                var normalized = lastName.Trim();
                query = query.Where(u => u.LastName.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                var normalized = nationalId.Trim();
                query = query.Where(u => u.NationalId != null && u.NationalId.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(columnFilterField) && !string.IsNullOrWhiteSpace(columnFilterValue))
            {
                var value = columnFilterValue.Trim();
                query = columnFilterField switch
                {
                    "username" => Repositories.QueryableColumnFilter.ApplyString(query, x => x.UserName, columnFilterOperator, value),
                    "email" => Repositories.QueryableColumnFilter.ApplyString(query, x => x.Email, columnFilterOperator, value),
                    "personnel" => Repositories.QueryableColumnFilter.ApplyString(query, x => x.FirstName + " " + x.LastName + " " + (x.NationalId ?? "") + " " + (x.PersonnelCode ?? ""), columnFilterOperator, value),
                    "phoneNumber" => Repositories.QueryableColumnFilter.ApplyString(query, x => x.PhoneNumber, columnFilterOperator, value),
                    "nationalId" => Repositories.QueryableColumnFilter.ApplyString(query, x => x.NationalId, columnFilterOperator, value),
                    "personnelCode" => Repositories.QueryableColumnFilter.ApplyString(query, x => x.PersonnelCode, columnFilterOperator, value),
                    "isActive" => ApplyActiveFilter(query, value, columnFilterOperator),
                    "twoFactorEnabled" => ApplyBooleanFilter(query, value, columnFilterOperator, x => x.TwoFactorEnabled),
                    "autoBackupEnabled" => ApplyBooleanFilter(query, value, columnFilterOperator, x => x.AutoBackupEnabled),
                    _ => query
                };
            }

            var normalizedSort = string.IsNullOrWhiteSpace(sortBy) ? "CreatedAt" : sortBy.Trim();
            if (!AllowedSortColumns.Contains(normalizedSort))
            {
                normalizedSort = "CreatedAt";
            }

            query = ApplySorting(query, normalizedSort, sortDescending);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListItemDto(
                    u.Id,
                    u.UserName ?? string.Empty,
                    u.Email ?? string.Empty,
                    u.PhoneNumber,
                    u.FirstName,
                    u.LastName,
                    u.NationalId,
                    u.PersonnelCode,
                    u.IsActive,
                    u.TwoFactorEnabled,
                    u.AutoBackupEnabled,
                    u.ActiveDuration))
                .ToListAsync(cancellationToken);

            return new PagedResponse<UserListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<UserDetailsDto?> GetUserByIdAsync(long userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserDetailsDto(
                    u.Id,
                    u.UserName ?? string.Empty,
                    u.Email ?? string.Empty,
                    u.PhoneNumber,
                    u.FirstName,
                    u.LastName,
                    u.NationalId,
                    u.PersonnelCode,
                    u.IsActive,
                    u.TwoFactorEnabled,
                    u.AutoBackupEnabled,
                    u.ActiveDuration,
                    u.CreatedBy,
                    u.CreatedAt,
                    u.LastModifiedBy,
                    u.LastModified))
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static IQueryable<ApplicationUser> ApplyActiveFilter(
            IQueryable<ApplicationUser> query,
            string value,
            string? filterOperator)
        {
            var isActive = value.Contains("فعال", StringComparison.OrdinalIgnoreCase) &&
                           !value.Contains("غیرفعال", StringComparison.OrdinalIgnoreCase)
                ? true
                : value.Contains("غیرفعال", StringComparison.OrdinalIgnoreCase) ? false
                : bool.TryParse(value, out var parsed) ? parsed
                : (bool?)null;
            if (isActive is null) return query.Where(_ => false);
            return filterOperator is "doesNotEqual" or "doesNotContain"
                ? query.Where(x => x.IsActive != isActive.Value)
                : query.Where(x => x.IsActive == isActive.Value);
        }

        private static IQueryable<ApplicationUser> ApplyBooleanFilter(
            IQueryable<ApplicationUser> query,
            string value,
            string? filterOperator,
            System.Linq.Expressions.Expression<Func<ApplicationUser, bool>> selector)
        {
            var state = value.Contains("فعال", StringComparison.OrdinalIgnoreCase) && !value.Contains("غیرفعال", StringComparison.OrdinalIgnoreCase)
                ? true
                : value.Contains("غیرفعال", StringComparison.OrdinalIgnoreCase) ? false
                : bool.TryParse(value, out var parsed) ? parsed
                : (bool?)null;
            if (state is null) return query.Where(_ => false);
            var body = filterOperator is "doesNotEqual" or "doesNotContain"
                ? System.Linq.Expressions.Expression.NotEqual(selector.Body, System.Linq.Expressions.Expression.Constant(state.Value))
                : System.Linq.Expressions.Expression.Equal(selector.Body, System.Linq.Expressions.Expression.Constant(state.Value));
            return query.Where(System.Linq.Expressions.Expression.Lambda<Func<ApplicationUser, bool>>(body, selector.Parameters));
        }

        public async Task<UserDetailsDto> CreateUserAsync(CreateUserManagementRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedUsername = request.Username.Trim();
            var normalizedEmail = request.Email.Trim();
            var normalizedPhoneNumber = NormalizeOptional(request.PhoneNumber);
            var normalizedNationalId = NormalizeOptional(request.NationalId);
            var normalizedPersonnelCode = NormalizeOptional(request.PersonnelCode);
            var roles = request.Roles?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (await _dbContext.Users.AsNoTracking().AnyAsync(u => u.UserName == normalizedUsername, cancellationToken))
            {
                throw new ConflictException($"نام کاربری «{normalizedUsername}» قبلاً ثبت شده است.");
            }

            if (await _dbContext.Users.AsNoTracking().AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
            {
                throw new ConflictException($"ایمیل «{normalizedEmail}» قبلاً ثبت شده است.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedPhoneNumber) &&
                await _dbContext.Users.AsNoTracking().AnyAsync(u => u.PhoneNumber == normalizedPhoneNumber, cancellationToken))
            {
                throw new ConflictException($"شماره همراه «{normalizedPhoneNumber}» قبلاً ثبت شده است.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedNationalId) &&
                await _dbContext.Users.AsNoTracking().AnyAsync(u => u.NationalId == normalizedNationalId, cancellationToken))
            {
                throw new ConflictException($"کد ملی «{normalizedNationalId}» قبلاً ثبت شده است.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedPersonnelCode) &&
                await _dbContext.Users.AsNoTracking().AnyAsync(u => u.PersonnelCode == normalizedPersonnelCode, cancellationToken))
            {
                throw new ConflictException($"کد پرسنلی «{normalizedPersonnelCode}» قبلاً ثبت شده است.");
            }

            if (roles.Length > 0)
            {
                await ValidateRolesExistAsync(roles, nameof(request.Roles), cancellationToken);
            }

            var user = new ApplicationUser
            {
                UserName = normalizedUsername,
                Email = normalizedEmail,
                PhoneNumber = normalizedPhoneNumber,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                NationalId = normalizedNationalId,
                PersonnelCode = normalizedPersonnelCode,
                ActiveDuration = request.ActiveDuration,
                AutoBackupEnabled = request.AutoBackupEnabled,
                IsActive = true,
                LockoutEnabled = true,
                TwoFactorEnabled = request.TwoFactorEnabled,
                PhoneNumberConfirmed = request.TwoFactorEnabled && normalizedPhoneNumber is not null,
                CreatedBy = NormalizeOptional(request.CreatedBy),
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                throw new ValidationException(createResult.Errors.Select(x =>
                    new FluentValidation.Results.ValidationFailure(nameof(request.Password), x.Description)));
            }

            if (roles.Length > 0)
            {
                IdentityResult addRoleResult;
                try
                {
                    addRoleResult = await _userManager.AddToRolesAsync(user, roles);
                }
                catch (InvalidOperationException ex)
                {
                    await _userManager.DeleteAsync(user);
                    throw new ValidationException(new[]
                    {
                        new FluentValidation.Results.ValidationFailure(nameof(request.Roles), ex.Message)
                    });
                }

                if (!addRoleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    throw new ValidationException(addRoleResult.Errors.Select(x =>
                        new FluentValidation.Results.ValidationFailure(nameof(request.Roles), x.Description)));
                }
            }

            return await GetUserByIdAsync(user.Id, cancellationToken)
                   ?? throw new NotFoundException("Created user could not be loaded.");
        }

        public async Task<UserDetailsDto?> UpdateUserAsync(UpdateUserManagementRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
            if (user is null)
            {
                return null;
            }

            var normalizedUsername = request.Username.Trim();
            var normalizedUserNameKey = _userManager.NormalizeName(normalizedUsername);
            var normalizedEmail = request.Email.Trim();
            var normalizedPhoneNumber = NormalizeOptional(request.PhoneNumber);
            var normalizedNationalId = NormalizeOptional(request.NationalId);
            var normalizedPersonnelCode = NormalizeOptional(request.PersonnelCode);

            if (await _dbContext.Users.AsNoTracking()
                    .AnyAsync(
                        candidate => candidate.Id != request.UserId && candidate.NormalizedUserName == normalizedUserNameKey,
                        cancellationToken))
            {
                throw new ConflictException($"نام کاربری «{normalizedUsername}» قبلاً ثبت شده است.");
            }

            if (await _dbContext.Users.AsNoTracking()
                    .AnyAsync(u => u.Id != request.UserId && u.Email == normalizedEmail, cancellationToken))
            {
                throw new ConflictException($"ایمیل «{normalizedEmail}» قبلاً ثبت شده است.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedPhoneNumber) &&
                await _dbContext.Users.AsNoTracking()
                    .AnyAsync(u => u.Id != request.UserId && u.PhoneNumber == normalizedPhoneNumber, cancellationToken))
            {
                throw new ConflictException($"شماره همراه «{normalizedPhoneNumber}» قبلاً ثبت شده است.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedNationalId) &&
                await _dbContext.Users.AsNoTracking()
                    .AnyAsync(u => u.Id != request.UserId && u.NationalId == normalizedNationalId, cancellationToken))
            {
                throw new ConflictException($"کد ملی «{normalizedNationalId}» قبلاً ثبت شده است.");
            }

            if (!string.IsNullOrWhiteSpace(normalizedPersonnelCode) &&
                await _dbContext.Users.AsNoTracking()
                    .AnyAsync(u => u.Id != request.UserId && u.PersonnelCode == normalizedPersonnelCode, cancellationToken))
            {
                throw new ConflictException($"کد پرسنلی «{normalizedPersonnelCode}» قبلاً ثبت شده است.");
            }

            user.UserName = normalizedUsername;
            user.Email = normalizedEmail;
            user.PhoneNumber = normalizedPhoneNumber;
            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.NationalId = normalizedNationalId;
            user.PersonnelCode = normalizedPersonnelCode;
            user.ActiveDuration = request.ActiveDuration;
            user.AutoBackupEnabled = request.AutoBackupEnabled;
            user.TwoFactorEnabled = request.TwoFactorEnabled;
            user.PhoneNumberConfirmed = request.TwoFactorEnabled && normalizedPhoneNumber is not null;
            user.LastModified = DateTime.UtcNow;
            user.LastModifiedBy = NormalizeOptional(request.LastModifiedBy);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new ValidationException(updateResult.Errors.Select(x =>
                    new FluentValidation.Results.ValidationFailure(nameof(UpdateUserManagementRequest), x.Description)));
            }

            return await GetUserByIdAsync(user.Id, cancellationToken);
        }

        public async Task<bool> ResetPasswordAsync(
            long userId,
            string newPassword,
            string? modifiedBy,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return false;
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);
            if (!resetResult.Succeeded)
            {
                throw new ValidationException(resetResult.Errors.Select(x =>
                    new FluentValidation.Results.ValidationFailure(nameof(newPassword), x.Description)));
            }

            user.LastModified = DateTime.UtcNow;
            user.LastModifiedBy = NormalizeOptional(modifiedBy);
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new ValidationException(updateResult.Errors.Select(x =>
                    new FluentValidation.Results.ValidationFailure(nameof(userId), x.Description)));
            }

            await _securityEventLogger.LogAsync(
                eventType: "UserPasswordReset",
                description: $"Password was reset for user '{user.UserName}'.",
                actorUserId: modifiedBy,
                targetUserId: user.Id.ToString(),
                metadata: new { user.UserName });

            return true;
        }

        public async Task<bool> SetUserStatusAsync(
    long userId,
    bool isActive,
    string? modifiedBy,
    CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user is null)
            {
                return false;
            }

            if (user.IsActive == isActive)
            {
                return true;
            }

            var previousStatus = user.IsActive;

            user.IsActive = isActive;
            user.LastModified = DateTime.UtcNow;
            user.LastModifiedBy = NormalizeOptional(modifiedBy);

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _securityEventLogger.LogAsync(
                eventType: isActive ? "UserActivated" : "UserDeactivated",
                description: $"User '{user.UserName}' was {(isActive ? "activated" : "deactivated")}.",
                actorUserId: modifiedBy,
                targetUserId: user.Id.ToString(),
                metadata: new
                {
                    PreviousStatus = previousStatus,
                    CurrentStatus = isActive
                });

            return true;
        }


        public async Task<bool> AssignRolesAsync(
            long userId,
            IReadOnlyCollection<string> roles,
            string? modifiedBy,
            CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
            {
                return false;
            }

            var normalizedRoles = roles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedRoles.Length > 0)
            {
                await ValidateRolesExistAsync(normalizedRoles, nameof(roles), cancellationToken);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.OrderBy(x => x).SequenceEqual(normalizedRoles.OrderBy(x => x), StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                throw new ValidationException(removeResult.Errors.Select(x =>
                    new FluentValidation.Results.ValidationFailure(nameof(roles), x.Description)));
            }

            if (normalizedRoles.Length > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, normalizedRoles);
                if (!addResult.Succeeded)
                {
                    throw new ValidationException(addResult.Errors.Select(x =>
                        new FluentValidation.Results.ValidationFailure(nameof(roles), x.Description)));
                }
            }

            user.LastModified = DateTime.UtcNow;
            user.LastModifiedBy = NormalizeOptional(modifiedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static IQueryable<ApplicationUser> ApplySorting(
            IQueryable<ApplicationUser> query,
            string sortBy,
            bool sortDescending)
        {
            return sortDescending
                ? query.OrderByDescending(x => EF.Property<object>(x, sortBy))
                : query.OrderBy(x => EF.Property<object>(x, sortBy));
        }

        private static string? NormalizeOptional(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private async Task ValidateRolesExistAsync(
            IReadOnlyCollection<string> requestedRoles,
            string propertyName,
            CancellationToken cancellationToken)
        {
            var existingRoleNames = await _dbContext.Roles
                .AsNoTracking()
                .Where(dbRole => dbRole.Name != null)
                .Select(dbRole => dbRole.Name!)
                .ToListAsync(cancellationToken);

            var unknownRoles = requestedRoles
                .Where(role => !existingRoleNames.Contains(role, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (unknownRoles.Length == 0)
            {
                return;
            }

            throw new ValidationException(
                unknownRoles.Select(role =>
                    new FluentValidation.Results.ValidationFailure(propertyName, $"Role '{role}' does not exist.")));
        }
    }
}
