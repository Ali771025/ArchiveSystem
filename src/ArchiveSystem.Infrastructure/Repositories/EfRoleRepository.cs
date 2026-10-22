using AccessControlRepositories = ArchiveSystem.Application.AccessControl;
using CommonRepositories = ArchiveSystem.Application.Common.Interfaces.Repositories;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class EfRoleRepository : AccessControlRepositories.IRoleRepository, CommonRepositories.IRoleRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfRoleRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.AccessRoles
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);
        }

        public Task<Role?> GetByIdAsync(long roleId, CancellationToken cancellationToken = default)
        {
            return _dbContext.AccessRoles.FirstOrDefaultAsync(x => x.Id == roleId, cancellationToken);
        }

        public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim();
            return _dbContext.AccessRoles.FirstOrDefaultAsync(x => x.Name == normalized, cancellationToken);
        }

        public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        {
            await _dbContext.AccessRoles.AddAsync(role, cancellationToken);
        }

        public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
        {
            _dbContext.AccessRoles.Update(role);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
        {
            _dbContext.AccessRoles.Remove(role);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
