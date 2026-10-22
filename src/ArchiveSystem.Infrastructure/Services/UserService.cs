// src/ArchiveSystem.Infrastructure/Services/UserService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Identity;

namespace ArchiveSystem.Infrastructure.Services
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(long userId);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<bool> HasPermissionAsync(long userId, long caseId, string permission);
        Task<bool> CanUserCreateAsync(long userId);
        Task<bool> AssignCaseAccessAsync(long userId, long caseId, IEnumerable<string> permissions);
        Task<bool> RemoveCaseAccessAsync(long userId, long caseId);
    }

    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ArchiveDbContext _context;

        public UserService(UserManager<ApplicationUser> userManager, ArchiveDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(long userId)
        {
            return await _userManager.FindByIdAsync(userId.ToString());
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> HasPermissionAsync(long userId, long caseId, string permission)
        {
            var userCaseAccess = await _context.UserCaseAccesses
                .Where(uc => uc.UserId == userId && uc.CaseId == caseId)
                .FirstOrDefaultAsync();

            if (userCaseAccess == null)
                return false;

            var permissions = userCaseAccess.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries);
            return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> CanUserCreateAsync(long userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            // Step 1 baseline: authenticated users can create unless explicitly blocked by access rules.
            return true;
        }

        public async Task<bool> AssignCaseAccessAsync(long userId, long caseId, IEnumerable<string> permissions)
        {
            var existingAccess = await _context.UserCaseAccesses
                .Where(uc => uc.UserId == userId && uc.CaseId == caseId)
                .FirstOrDefaultAsync();

            if (existingAccess != null)
            {
                existingAccess.Permissions = string.Join(",", permissions);
                existingAccess.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var newAccess = new UserCaseAccess
                {
                    UserId = userId,
                    CaseId = caseId,
                    Permissions = string.Join(",", permissions),
                    CreatedAt = DateTime.UtcNow
                };
                await _context.UserCaseAccesses.AddAsync(newAccess);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCaseAccessAsync(long userId, long caseId)
        {
            var userCaseAccess = await _context.UserCaseAccesses
                .Where(uc => uc.UserId == userId && uc.CaseId == caseId)
                .FirstOrDefaultAsync();

            if (userCaseAccess != null)
            {
                _context.UserCaseAccesses.Remove(userCaseAccess);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}
