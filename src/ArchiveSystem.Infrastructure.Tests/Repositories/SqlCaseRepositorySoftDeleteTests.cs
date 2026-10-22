using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Repositories
{
    public sealed class SqlCaseRepositorySoftDeleteTests : IDisposable
    {
        private readonly ArchiveDbContext _context;
        private readonly SqlCaseRepository _repository;

        public SqlCaseRepositorySoftDeleteTests()
        {
            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseInMemoryDatabase($"case-soft-delete-{Guid.NewGuid():N}")
                .Options;

            _context = new ArchiveDbContext(options);
            _repository = new SqlCaseRepository(
                _context,
                new NoOpAuditService(),
                new HttpContextAccessor(),
                NullLogger<SqlCaseRepository>.Instance);
        }

        [Fact]
        public async Task DeleteAsync_SoftDeletesCase_AndRemovesItFromMainList()
        {
            var classification = new Classification(10, "Root");
            var activeCase = new Case(1, "CASE-1", "Active case", classificationId: 10);
            var deletedCase = new Case(2, "CASE-2", "Case to delete", classificationId: 10);
            await _context.Classifications.AddAsync(classification);
            await _context.Cases.AddRangeAsync(activeCase, deletedCase);
            await _context.SaveChangesAsync();

            await _repository.DeleteAsync(deletedCase);
            await _repository.SaveChangesAsync();

            var storedDeletedCase = await _context.Cases.IgnoreQueryFilters().SingleAsync(x => x.Id == 2);
            Assert.True(storedDeletedCase.IsDeleted);
            Assert.NotNull(storedDeletedCase.DeletedAt);

            Assert.Null(await _repository.GetByIdAsync(2));
            Assert.Null(await _repository.GetByCaseNumberAsync("CASE-2"));
            Assert.False(await _repository.ExistsAsync(2));

            var mainList = await _repository.GetAllAsync();
            Assert.Single(mainList);
            Assert.Equal("CASE-1", mainList[0].CaseNumber);
            Assert.Equal(1, await _repository.CountAsync());
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed class NoOpAuditService : IAuditService
        {
            public Task LogOperationAsync(
                long userId,
                string userName,
                string action,
                string tableName,
                string recordId,
                string? oldValue,
                string? newValue,
                string ipAddress,
                string userAgent,
                string severity = "Information")
            {
                return Task.CompletedTask;
            }

            public Task LogFileOperationAsync(
                long userId,
                string userName,
                ArchiveFile file,
                string action,
                string ipAddress,
                string userAgent)
            {
                return Task.CompletedTask;
            }

            public Task LogCaseOperationAsync(
                long userId,
                string userName,
                Case caseEntity,
                string action,
                string ipAddress,
                string userAgent)
            {
                return Task.CompletedTask;
            }
        }
    }
}
