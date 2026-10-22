using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Tests.Repositories;

public sealed class EfCaseLoanRepositoryTests
{
    [Theory]
    [InlineData("1405-001")]
    [InlineData("REF-7788")]
    public async Task GetPagedAsync_FindsLoanByCaseNumberOrExternalCaseNumber(string searchTerm)
    {
        var options = new DbContextOptionsBuilder<ArchiveDbContext>()
            .UseInMemoryDatabase($"case-loan-search-{Guid.NewGuid():N}")
            .Options;

        await using var dbContext = new ArchiveDbContext(options);
        dbContext.Cases.Add(new Case(
            101,
            "1405-001",
            "پرونده آزمون",
            1,
            externalCaseNumber: "REF-7788"));
        dbContext.CaseLoans.Add(new CaseLoan(
            Guid.NewGuid(),
            "علی",
            "آزمون",
            "1405-001",
            "0012345678",
            "09120000000",
            new DateTime(2026, 8, 2),
            new DateTime(2026, 8, 3),
            null,
            null,
            "Loaned",
            null,
            "کاربر آزمون"));
        await dbContext.SaveChangesAsync();

        var repository = new EfCaseLoanRepository(dbContext);

        var result = await repository.GetPagedAsync(1, 20, searchTerm, null, null);

        var item = Assert.Single(result.Items);
        Assert.Equal("1405-001", item.CaseNumber);
        Assert.Equal(1, result.TotalCount);
    }
}
