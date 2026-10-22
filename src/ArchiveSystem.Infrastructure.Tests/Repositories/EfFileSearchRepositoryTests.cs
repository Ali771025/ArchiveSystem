using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.ValueObjects;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Repositories;

public sealed class EfFileSearchRepositoryTests : IDisposable
{
    private readonly ArchiveDbContext _dbContext;
    private readonly EfFileSearchRepository _repository;

    public EfFileSearchRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ArchiveDbContext>()
            .UseInMemoryDatabase($"FileSearch_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ArchiveDbContext(options);
        _repository = new EfFileSearchRepository(_dbContext);
    }

    [Fact]
    public async Task SearchAsync_ReturnsTaggedFile_WhenClassificationIsReadable()
    {
        const long caseId = 10;
        const long fileId = 20;
        const long firstTagId = 30;
        const long secondTagId = 31;

        _dbContext.Cases.Add(new Case(caseId, "CASE-10", "Case 10", classificationId: 1));
        _dbContext.Files.Add(new ArchiveFile(
            fileId,
            "tagged.pdf",
            FileSize.Create(1024),
            FileExtension.Create(".pdf"),
            "application/pdf",
            "/files/tagged.pdf",
            caseId));
        _dbContext.Tags.AddRange(
            new Tag(firstTagId, "important"),
            new Tag(secondTagId, "reviewed"));
        await _dbContext.SaveChangesAsync();

        var fileTagRepository = new EfFileTagRepository(_dbContext);
        await fileTagRepository.SetFileTagsAsync(
            fileId,
            new[] { firstTagId, secondTagId });
        _dbContext.ChangeTracker.Clear();

        var result = await _repository.SearchAsync(
            term: null,
            caseId,
            fromDate: null,
            toDate: null,
            page: 1,
            pageSize: 20,
            sortBy: null,
            sortAscending: true,
            allowedClassificationIds: new[] { 1L },
            bypassAuthorizationFilters: false);

        var file = Assert.Single(result.Items);
        Assert.Equal(
            new[] { firstTagId, secondTagId },
            file.Tags.OrderBy(tagId => tagId));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
