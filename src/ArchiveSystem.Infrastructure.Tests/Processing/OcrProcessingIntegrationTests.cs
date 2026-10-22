using System.Text.Json;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Domain.ValueObjects;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using ArchiveSystem.Worker.Ocr.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using FileEntity = ArchiveSystem.Domain.Entities.ArchiveFile;

namespace ArchiveSystem.Infrastructure.Tests.Processing
{
    public class OcrProcessingIntegrationTests
    {
        [Fact]
        public async Task ProcessFileVersionAsync_StoresOcrJsonAndMarksCompleted()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new ArchiveDbContext(dbOptions);
            await dbContext.Database.EnsureCreatedAsync();

            var file = new FileEntity(
                id: 0,
                name: "ocr-source.pdf",
                size: FileSize.Create(512),
                extension: FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "seed/ocr-source.pdf",
                caseId: null);
            await dbContext.Files.AddAsync(file);
            await dbContext.SaveChangesAsync();

            var version = new FileVersion(
                id: 0,
                fileId: file.Id,
                versionNumber: 1,
                storagePath: "scanner/ocr-source.pdf",
                fileName: "ocr-source.pdf",
                contentType: "application/pdf",
                size: 512,
                sha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            await dbContext.FileVersions.AddAsync(version);
            await dbContext.SaveChangesAsync();

            var repository = new EfFileVersionRepository(dbContext);
            var storage = new FakeObjectStorage(new Dictionary<string, byte[]>
            {
                ["scanner/ocr-source.pdf"] = new byte[] { 1, 2, 3, 4 }
            });
            var messageBus = new CapturingMessageBus();
            var ocrService = new FakeOcrService("hello world");
            var processor = new OcrProcessingService(
                repository,
                storage,
                Options.Create(new ObjectStorageOptions { BucketName = "archive-system" }),
                Options.Create(new ArchiveSystem.Worker.Ocr.Options.OcrWorkerOptions { JobTimeoutMinutes = 0 }),
                ocrService,
                messageBus,
                NullLogger<OcrProcessingService>.Instance);

            await processor.ProcessFileVersionAsync(version.Id, CancellationToken.None);

            var updated = await repository.GetByIdAsync(version.Id, CancellationToken.None);
            Assert.NotNull(updated);
            Assert.Equal(global::ArchiveSystem.Domain.Enums.ProcessingStatus.Completed, updated!.OcrStatus);
            Assert.NotNull(updated.OcrResult);
            using var ocrJson = JsonDocument.Parse(updated.OcrResult);
            Assert.Equal("hello world", ocrJson.RootElement.GetProperty("text").GetString());
            Assert.Equal("en", updated.OcrLanguage);
            Assert.Equal(2, updated.OcrWordCount);

            var published = Assert.Single(messageBus.PublishedEvents.OfType<OcrCompletedEvent>());
            Assert.Equal(file.Id, published.FileId);
            Assert.Equal(version.Id, published.FileVersionId);
            Assert.True(published.HasText);
        }

        [Fact]
        public async Task ResetStaleOcrProcessingAsync_RequeuesExpiredProcessingVersions()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new ArchiveDbContext(dbOptions);
            await dbContext.Database.EnsureCreatedAsync();

            var file = new FileEntity(
                id: 0,
                name: "stale.pdf",
                size: FileSize.Create(512),
                extension: FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "seed/stale.pdf",
                caseId: null);
            await dbContext.Files.AddAsync(file);
            await dbContext.SaveChangesAsync();

            var version = new FileVersion(
                id: 0,
                fileId: file.Id,
                versionNumber: 1,
                storagePath: "scanner/stale.pdf",
                fileName: "stale.pdf",
                contentType: "application/pdf",
                size: 512,
                sha256: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
            await dbContext.FileVersions.AddAsync(version);
            await dbContext.SaveChangesAsync();

            var repository = new EfFileVersionRepository(dbContext);
            Assert.True(await repository.TryStartOcrProcessingAsync(version.Id));

            await dbContext.FileVersions
                .Where(x => x.Id == version.Id)
                .ExecuteUpdateAsync(updates => updates.SetProperty(x => x.ModifiedAt, DateTime.UtcNow.AddHours(-2)));

            var resetCount = await repository.ResetStaleOcrProcessingAsync(
                TimeSpan.FromMinutes(30),
                "reset by test",
                CancellationToken.None);

            var updated = await repository.GetByIdAsync(version.Id);
            Assert.Equal(1, resetCount);
            Assert.NotNull(updated);
            Assert.Equal(ProcessingStatus.Pending, updated!.OcrStatus);
            Assert.Equal("reset by test", updated.OcrError);
        }

        private sealed class FakeOcrService : IOcrService
        {
            private readonly string _text;

            public FakeOcrService(string text)
            {
                _text = text;
            }

            public Task<OcrResult> ExtractAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new OcrResult
                {
                    Engine = "fake",
                    Language = "en",
                    Confidence = 1f,
                    PageCount = 1,
                    WordCount = 2,
                    ProcessingDurationMs = 1,
                    HasText = true,
                    Text = _text
                });
            }
        }

        private sealed class CapturingMessageBus : IMessageBus
        {
            public List<object> PublishedEvents { get; } = new();

            public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
            {
                PublishedEvents.Add(message);
                return Task.CompletedTask;
            }

            public Task PublishAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default) where T : class
            {
                PublishedEvents.AddRange(messages.Cast<object>());
                return Task.CompletedTask;
            }

            public Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, string queueName, CancellationToken cancellationToken = default) where T : class
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakeObjectStorage : IObjectStorage
        {
            private readonly IDictionary<string, byte[]> _objects;

            public FakeObjectStorage(IDictionary<string, byte[]> objects)
            {
                _objects = objects;
            }

            public Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<string> InitiateMultipartUploadAsync(string bucketName, string objectKey, string contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task<UploadPartResult> UploadPartAsync(string bucketName, string objectKey, string uploadId, int partNumber, Stream content, long contentLength, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task CompleteMultipartUploadAsync(string bucketName, string objectKey, string uploadId, IReadOnlyCollection<MultipartPartETag> parts, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task AbortMultipartUploadAsync(string bucketName, string objectKey, string uploadId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<Stream> GetObjectStreamAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
            {
                if (!_objects.TryGetValue(objectKey, out var value))
                {
                    throw new FileNotFoundException();
                }

                return Task.FromResult<Stream>(new MemoryStream(value));
            }

            public Task<ObjectStat> GetObjectStatAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
            {
                if (!_objects.TryGetValue(objectKey, out var value))
                {
                    throw new FileNotFoundException();
                }

                return Task.FromResult(new ObjectStat(value.Length, "application/octet-stream"));
            }

            public Task PutObjectAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default) => throw new NotSupportedException();

            public Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_objects.ContainsKey(objectKey));
            }
        }
    }
}
