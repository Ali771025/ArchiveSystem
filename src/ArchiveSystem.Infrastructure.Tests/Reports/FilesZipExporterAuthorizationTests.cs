using System.IO.Compression;
using System.Text;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.ValueObjects;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using ArchiveSystem.Infrastructure.Services.Exports;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Reports
{
    public class FilesZipExporterAuthorizationTests
    {
        [Fact]
        public async Task GenerateAsync_UserWithoutTagAccess_ExcludesUnauthorizedFiles()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var dbOptions = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new ArchiveDbContext(dbOptions);
            await dbContext.Database.EnsureCreatedAsync();
            await SeedAsync(dbContext);

            var objectStorage = new Mock<IObjectStorage>();
            objectStorage
                .Setup(x => x.GetObjectStreamAsync("archive-system", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, string key, CancellationToken _) =>
                {
                    var bytes = Encoding.UTF8.GetBytes($"content-for:{key}");
                    return new MemoryStream(bytes);
                });

            var repository = new EfFileExportQueryRepository(dbContext);
            var exporter = new FilesZipExporter(
                repository,
                objectStorage.Object,
                Options.Create(new ObjectStorageOptions { BucketName = "archive-system" }),
                NullLogger<FilesZipExporter>.Instance);

            var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");
            try
            {
                var exported = await exporter.GenerateAsync(
                    outputPath,
                    new FilesZipExportRequest { ClassificationId = 1 },
                    new ActivityReportContext
                    {
                        IsAdmin = false,
                        AllowedClassificationIds = new[] { 1L },
                        AllowedTagIds = new[] { 10L }
                    },
                    CancellationToken.None);

                Assert.Equal(1, exported);

                using var zip = ZipFile.OpenRead(outputPath);
                var fileEntries = zip.Entries.Where(x => x.FullName.StartsWith("files/", StringComparison.OrdinalIgnoreCase)).ToList();
                Assert.Single(fileEntries);
                Assert.Contains("authorized.pdf", fileEntries[0].FullName, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(fileEntries, x => x.FullName.Contains("blocked", StringComparison.OrdinalIgnoreCase));

                Assert.Contains(zip.Entries, x => x.FullName == "metadata/exported-files.csv");
            }
            finally
            {
                if (System.IO.File.Exists(outputPath))
                {
                    System.IO.File.Delete(outputPath);
                }
            }
        }

        private static async Task SeedAsync(ArchiveDbContext dbContext)
        {
            var classification = new Classification(1, "Root");
            var caseEntity = new Case(200, "CASE-200", "Case 200", 1);
            var fileAllowed = new global::ArchiveSystem.Domain.Entities.ArchiveFile(
                id: 300,
                name: "authorized.pdf",
                size: FileSize.Create(1024),
                extension: FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "files/allowed",
                caseId: 200);
            var fileBlocked = new global::ArchiveSystem.Domain.Entities.ArchiveFile(
                id: 301,
                name: "blocked.pdf",
                size: FileSize.Create(1024),
                extension: FileExtension.Create(".pdf"),
                contentType: "application/pdf",
                storagePath: "files/blocked",
                caseId: 200);

            var versionAllowed = new FileVersion(
                id: 400,
                fileId: 300,
                versionNumber: 1,
                storagePath: "obj/allowed",
                fileName: "authorized.pdf",
                contentType: "application/pdf",
                size: 1024,
                sha256: "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var versionBlocked = new FileVersion(
                id: 401,
                fileId: 301,
                versionNumber: 1,
                storagePath: "obj/blocked",
                fileName: "blocked.pdf",
                contentType: "application/pdf",
                size: 1024,
                sha256: "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");

            fileAllowed.ApplyLatestVersionMetadata(versionAllowed.Id, versionAllowed.StoragePath, versionAllowed.Size, versionAllowed.ContentType, versionAllowed.Sha256);
            fileBlocked.ApplyLatestVersionMetadata(versionBlocked.Id, versionBlocked.StoragePath, versionBlocked.Size, versionBlocked.ContentType, versionBlocked.Sha256);

            await dbContext.Classifications.AddAsync(classification);
            await dbContext.Cases.AddAsync(caseEntity);
            await dbContext.Files.AddRangeAsync(fileAllowed, fileBlocked);
            await dbContext.FileVersions.AddRangeAsync(versionAllowed, versionBlocked);
            await dbContext.Tags.AddRangeAsync(
                new Tag(10, "Allow"),
                new Tag(20, "Deny"));
            await dbContext.FileTags.AddRangeAsync(
                new FileTag { FileId = 300, TagId = 10 },
                new FileTag { FileId = 301, TagId = 20 });

            await dbContext.SaveChangesAsync();
        }
    }
}
