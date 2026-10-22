using AccessControlRepositories = ArchiveSystem.Application.AccessControl;
using CommonRepositories = ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfUserRoleRepository : AccessControlRepositories.IUserRoleRepository, CommonRepositories.IUserRoleRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfUserRoleRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<long>> GetRoleIdsByUserIdAsync(long userId, CancellationToken cancellationToken = default)
        {
            var identityRoleIds = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.RoleId)
                .ToArrayAsync(cancellationToken);

            var legacyRoleIds = await _dbContext.AccessUserRoles
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.RoleId)
                .ToArrayAsync(cancellationToken);

            return identityRoleIds
                .Concat(legacyRoleIds)
                .Distinct()
                .ToArray();
        }

        public async Task<IReadOnlyCollection<long>> GetUserIdsByRoleIdsAsync(IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default)
        {
            var set = roleIds.Distinct().ToArray();
            if (set.Length == 0)
            {
                return Array.Empty<long>();
            }

            var identityUserIds = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(x => set.Contains(x.RoleId))
                .Select(x => x.UserId)
                .ToArrayAsync(cancellationToken);

            var legacyUserIds = await _dbContext.AccessUserRoles
                .AsNoTracking()
                .Where(x => set.Contains(x.RoleId))
                .Select(x => x.UserId)
                .ToArrayAsync(cancellationToken);

            return identityUserIds
                .Concat(legacyUserIds)
                .Distinct()
                .ToArray();
        }

        public async Task SetUserRolesAsync(long userId, IReadOnlyCollection<long> roleIds, CancellationToken cancellationToken = default)
        {
            var targetRoleIds = roleIds
                .Where(roleId => roleId > 0)
                .Distinct()
                .ToHashSet();

            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var existingEntries = await _dbContext.AccessUserRoles
                        .Where(x => x.UserId == userId)
                        .ToListAsync(cancellationToken);

                    var existingRoleIds = existingEntries
                        .Select(entry => entry.RoleId)
                        .ToHashSet();

                    var rolesToRemove = existingEntries
                        .Where(entry => !targetRoleIds.Contains(entry.RoleId))
                        .ToList();

                    if (rolesToRemove.Count > 0)
                    {
                        _dbContext.AccessUserRoles.RemoveRange(rolesToRemove);
                    }

                    var rolesToAdd = targetRoleIds
                        .Where(roleId => !existingRoleIds.Contains(roleId))
                        .ToArray();

                    foreach (var roleId in rolesToAdd)
                    {
                        _dbContext.AccessUserRoles.Add(new UserRole
                        {
                            UserId = userId,
                            RoleId = roleId
                        });
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
    }
}
