using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Services;

public sealed class AuditServiceFileHistoryTests
{
    [Fact]
    public async Task LogOperationAsync_FileVersionPayload_MapsFileCaseAndClassificationToMovementLog()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ArchiveDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ArchiveDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var writer = new MovementAuditWriter(context);
        var service = new AuditService(context, new HttpContextAccessor(), writer);

        await service.LogOperationAsync(
            userId: 500,
            userName: "operator",
            action: "UPLOAD_FINALIZED",
            tableName: "FileVersions",
            recordId: "300",
            oldValue: null,
            newValue: """
                {"fileId":200,"caseId":100,"classificationId":57,"versionId":300}
                """,
            ipAddress: "127.0.0.1",
            userAgent: "test");

        var movementLog = await context.MovementLogs.SingleAsync();
        Assert.Equal("UPLOAD_FINALIZED", movementLog.ActionType);
        Assert.Equal(200, movementLog.FileId);
        Assert.Equal(300, movementLog.FileVersionId);
        Assert.Equal(100, movementLog.CaseId);
        Assert.Equal(57, movementLog.ClassificationId);
    }

    [Fact]
    public async Task LogOperationAsync_FileDeletePayload_MapsCaseToMovementLog()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ArchiveDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ArchiveDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var service = new AuditService(
            context,
            new HttpContextAccessor(),
            new MovementAuditWriter(context));

        await service.LogOperationAsync(
            userId: 500,
            userName: "operator",
            action: "SOFT_DELETE",
            tableName: "Files",
            recordId: "200",
            oldValue: """
                {"Id":200,"CaseId":100,"ClassificationId":57}
                """,
            newValue: """
                {"Id":200,"CaseId":100,"ClassificationId":57,"DeletedAt":"2026-07-21T00:00:00Z"}
                """,
            ipAddress: "127.0.0.1",
            userAgent: "test");

        var movementLog = await context.MovementLogs.SingleAsync();
        Assert.Equal("SOFT_DELETE", movementLog.ActionType);
        Assert.Equal(200, movementLog.FileId);
        Assert.Equal(100, movementLog.CaseId);
        Assert.Equal(57, movementLog.ClassificationId);
    }
}
