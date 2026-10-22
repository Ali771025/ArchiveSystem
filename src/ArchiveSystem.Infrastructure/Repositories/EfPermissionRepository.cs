using AccessControlRepositories = ArchiveSystem.Application.AccessControl;
using CommonRepositories = ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfPermissionRepository : AccessControlRepositories.IPermissionRepository, CommonRepositories.IPermissionRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfPermissionRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.Permissions
                .AsNoTracking()
                .OrderBy(x => x.Key)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Permission>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default)
        {
            var permissionIds = await _dbContext.RolePermissions
                .AsNoTracking()
                .Where(x => x.RoleId == roleId)
                .Select(x => x.PermissionId)
                .ToListAsync(cancellationToken);

            return await _dbContext.Permissions
                .AsNoTracking()
                .Where(x => permissionIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task SetRolePermissionsAsync(long roleId, IReadOnlyCollection<long> permissionIds, CancellationToken cancellationToken = default)
        {
            var existing = _dbContext.RolePermissions.Where(x => x.RoleId == roleId);
            _dbContext.RolePermissions.RemoveRange(existing);

            var distinctPermissionIds = permissionIds.Distinct().ToArray();
            foreach (var permissionId in distinctPermissionIds)
            {
                _dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
