using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.AccessControl
{
    public sealed class EfUserRoleRepositoryTests
    {
        [Fact]
        public async Task GetRoleIdsByUserIdAsync_IncludesIdentityRoleAssignments()
        {
            await using var dbContext = CreateDbContext();
            dbContext.UserRoles.Add(new IdentityUserRole<long>
            {
                UserId = 42,
                RoleId = 100
            });
            dbContext.AccessUserRoles.Add(new UserRole
            {
                UserId = 42,
                RoleId = 200
            });
            await dbContext.SaveChangesAsync();

            var repository = new EfUserRoleRepository(dbContext);

            var roleIds = await repository.GetRoleIdsByUserIdAsync(42);

            Assert.Equal(new long[] { 100, 200 }, roleIds.OrderBy(id => id));
        }

        [Fact]
        public async Task GetUserIdsByRoleIdsAsync_IncludesIdentityRoleAssignments()
        {
            await using var dbContext = CreateDbContext();
            dbContext.UserRoles.Add(new IdentityUserRole<long>
            {
                UserId = 42,
                RoleId = 100
            });
            await dbContext.SaveChangesAsync();

            var repository = new EfUserRoleRepository(dbContext);

            var userIds = await repository.GetUserIdsByRoleIdsAsync(new long[] { 100 });

            Assert.Equal(new long[] { 42 }, userIds);
        }

        private static ArchiveDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseInMemoryDatabase($"identity-role-acl-{Guid.NewGuid():N}")
                .Options;

            return new ArchiveDbContext(options);
        }
    }
}
