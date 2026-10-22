using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Reports
{
    public class MovementLogReportAuthorizationTests
    {
        [Fact]
        public async Task QueryActivity_AppliesAuthorizationAndExecutesSqlQuery()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var sqlLogs = new List<string>();
            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .LogTo(message => sqlLogs.Add(message), LogLevel.Information)
                .Options;

            await using var dbContext = new ArchiveDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            await SeedAsync(dbContext);

            var repository = new EfMovementLogRepository(dbContext);
            var result = await repository.QueryActivityAsync(
                new ActivityReportFilter
                {
                    ActionType = "FILE_DOWNLOADED",
                    Page = 1,
                    PageSize = 20
                },
                new ActivityReportContext
                {
                    IsAdmin = false,
                    AllowedClassificationIds = new[] { 1L },
                    AllowedTagIds = new[] { 10L }
                },
                CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal(100L, result.Items[0].FileId);
            Assert.Contains(sqlLogs, x => x.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(sqlLogs, x => x.Contains("MovementLogs", StringComparison.OrdinalIgnoreCase));
        }

        private static async Task SeedAsync(ArchiveDbContext dbContext)
        {
            var root = new global::ArchiveSystem.Domain.Entities.Classification(1, "Root");
            var child = new global::ArchiveSystem.Domain.Entities.Classification(2, "Child", parentId: 1);
            var case1 = new global::ArchiveSystem.Domain.Entities.Case(11, "CASE-11", "Case11", 1);
            var case2 = new global::ArchiveSystem.Domain.Entities.Case(12, "CASE-12", "Case12", 2);

            var file1 = new global::ArchiveSystem.Domain.Entities.ArchiveFile(
                id: 100,
                name: "allowed.pdf",
                size: global::ArchiveSystem.Domain.ValueObjects.FileSize.Create(100),
                extension: global::ArchiveSystem.Domain.ValueObjects.FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "files/100",
                caseId: 11);
            var file2 = new global::ArchiveSystem.Domain.Entities.ArchiveFile(
                id: 200,
                name: "blocked.pdf",
                size: global::ArchiveSystem.Domain.ValueObjects.FileSize.Create(100),
                extension: global::ArchiveSystem.Domain.ValueObjects.FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "files/200",
                caseId: 12);

            await dbContext.Classifications.AddRangeAsync(root, child);
            await dbContext.Cases.AddRangeAsync(case1, case2);
            await dbContext.Files.AddRangeAsync(file1, file2);
            await dbContext.Tags.AddRangeAsync(
                new global::ArchiveSystem.Domain.Entities.Tag(10, "Tag10"),
                new global::ArchiveSystem.Domain.Entities.Tag(20, "Tag20"));
            await dbContext.FileTags.AddRangeAsync(
                new global::ArchiveSystem.Domain.Entities.FileTag { FileId = 100, TagId = 10 },
                new global::ArchiveSystem.Domain.Entities.FileTag { FileId = 200, TagId = 20 });

            await dbContext.MovementLogs.AddRangeAsync(
                new global::ArchiveSystem.Domain.Entities.MovementLog(
                    actorUserId: 1,
                    actionType: "FILE_DOWNLOADED",
                    entityType: "File",
                    entityId: "100",
                    classificationId: 1,
                    fileId: 100,
                    tagId: 10),
                new global::ArchiveSystem.Domain.Entities.MovementLog(
                    actorUserId: 1,
                    actionType: "FILE_DOWNLOADED",
                    entityType: "File",
                    entityId: "200",
                    classificationId: 2,
                    fileId: 200,
                    tagId: 20));

            await dbContext.SaveChangesAsync();
        }
    }
}
