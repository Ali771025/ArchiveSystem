using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.ValueObjects;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Infrastructure.Tests.Processing
{
    public class FileVersionProcessingIdempotencyTests
    {
        [Fact]
        public async Task OcrCompletion_IsIdempotent_WhenProcessedTwice()
        {
            await using var fixture = await SqliteFixture.CreateAsync();
            var repository = new EfFileVersionRepository(fixture.DbContext);
            var version = await fixture.CreateFileVersionAsync();

            var started = await repository.TryStartOcrProcessingAsync(version.Id, CancellationToken.None);
            Assert.True(started);

            var completed = await repository.TryCompleteOcrAsync(version.Id, "{\"text\":\"first\"}", "first", "en", 1, CancellationToken.None);
            Assert.True(completed);

            var startedAgain = await repository.TryStartOcrProcessingAsync(version.Id, CancellationToken.None);
            var completedAgain = await repository.TryCompleteOcrAsync(version.Id, "{\"text\":\"second\"}", "second", "en", 2, CancellationToken.None);

            Assert.False(startedAgain);
            Assert.False(completedAgain);

            var reloaded = await repository.GetByIdAsync(version.Id, CancellationToken.None);
            Assert.NotNull(reloaded);
            Assert.Equal(ProcessingStatus.Completed, reloaded!.OcrStatus);
            Assert.Equal("{\"text\":\"first\"}", reloaded.OcrResult);
            Assert.Equal("first", reloaded.OcrText);
            Assert.Equal(1, reloaded.OcrWordCount);
        }

        [Fact]
        public async Task OcrRetry_OnlyQueuesFailedVersionAndClearsFailure()
        {
            await using var fixture = await SqliteFixture.CreateAsync();
            var repository = new EfFileVersionRepository(fixture.DbContext);
            var version = await fixture.CreateFileVersionAsync();

            Assert.True(await repository.TryStartOcrProcessingAsync(version.Id, CancellationToken.None));
            Assert.True(await repository.TryFailOcrAsync(version.Id, "temporary failure", CancellationToken.None));
            Assert.True(await repository.TryQueueOcrRetryAsync(version.Id, CancellationToken.None));
            Assert.False(await repository.TryQueueOcrRetryAsync(version.Id, CancellationToken.None));

            var reloaded = await repository.GetByIdAsync(version.Id, CancellationToken.None);
            Assert.NotNull(reloaded);
            Assert.Equal(ProcessingStatus.Pending, reloaded!.OcrStatus);
            Assert.Null(reloaded.OcrError);
        }

        [Fact]
        public async Task ThumbnailCompletion_IsIdempotent_WhenProcessedTwice()
        {
            await using var fixture = await SqliteFixture.CreateAsync();
            var repository = new EfFileVersionRepository(fixture.DbContext);
            var version = await fixture.CreateFileVersionAsync();

            var started = await repository.TryStartThumbnailProcessingAsync(version.Id, CancellationToken.None);
            Assert.True(started);

            var bytesFirst = new byte[] { 1, 2, 3 };
            var completed = await repository.TryCompleteThumbnailAsync(version.Id, bytesFirst, "image/jpeg", 64, 64, CancellationToken.None);
            Assert.True(completed);

            var startedAgain = await repository.TryStartThumbnailProcessingAsync(version.Id, CancellationToken.None);
            var completedAgain = await repository.TryCompleteThumbnailAsync(version.Id, new byte[] { 8, 9 }, "image/jpeg", 32, 32, CancellationToken.None);

            Assert.False(startedAgain);
            Assert.False(completedAgain);

            var reloaded = await repository.GetByIdAsync(version.Id, CancellationToken.None);
            Assert.NotNull(reloaded);
            Assert.Equal(ProcessingStatus.Completed, reloaded!.ThumbnailStatus);
            Assert.Equal(bytesFirst, reloaded.Thumbnail);
            Assert.Equal(64, reloaded.ThumbnailWidth);
            Assert.Equal(64, reloaded.ThumbnailHeight);
        }

        private sealed class SqliteFixture : IAsyncDisposable
        {
            private readonly SqliteConnection _connection;
            public ArchiveDbContext DbContext { get; }

            private SqliteFixture(SqliteConnection connection, ArchiveDbContext dbContext)
            {
                _connection = connection;
                DbContext = dbContext;
            }

            public static async Task<SqliteFixture> CreateAsync()
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                await connection.OpenAsync();

                var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                    .UseSqlite(connection)
                    .Options;

                var dbContext = new ArchiveDbContext(options);
                await dbContext.Database.EnsureCreatedAsync();

                return new SqliteFixture(connection, dbContext);
            }

            public async Task<FileVersion> CreateFileVersionAsync()
            {
                var file = new FileEntity(
                    id: 0,
                    name: "doc.pdf",
                    size: FileSize.Create(100),
                    extension: FileExtension.Create(".pdf"),
                    contentType: "application/pdf",
                    storagePath: "seed/doc.pdf",
                    caseId: null);

                await DbContext.Files.AddAsync(file);
                await DbContext.SaveChangesAsync();

                var version = new FileVersion(
                    id: 0,
                    fileId: file.Id,
                    versionNumber: 1,
                    storagePath: "seed/version/doc.pdf",
                    fileName: "doc.pdf",
                    contentType: "application/pdf",
                    size: 100,
                    sha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");

                await DbContext.FileVersions.AddAsync(version);
                await DbContext.SaveChangesAsync();
                return version;
            }

            public async ValueTask DisposeAsync()
            {
                await DbContext.DisposeAsync();
                await _connection.DisposeAsync();
            }
        }
    }
}
