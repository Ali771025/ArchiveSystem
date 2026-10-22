using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Repositories;
using ArchiveSystem.Infrastructure.Services.Exports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Reports
{
    public class ExportServiceAccessControlTests
    {
        [Fact]
        public async Task GetJobDownload_OnlyOwnerOrAdminCanDownload()
        {
            var options = new DbContextOptionsBuilder<ArchiveDbContext>()
                .UseInMemoryDatabase($"export-job-access-{Guid.NewGuid():N}")
                .Options;

            await using var dbContext = new ArchiveDbContext(options);
            var repository = new EfExportJobRepository(dbContext);
            var storageService = new Mock<IStorageService>(MockBehavior.Strict);

            var exportService = new ExportService(
                repository,
                storageService.Object,
                Options.Create(new ObjectStorageOptions { BucketName = "archive-system" }));

            var job = new ExportJob(
                id: Guid.NewGuid(),
                createdByUserId: 101,
                type: global::ArchiveSystem.Domain.Enums.ExportJobType.ActivityExcel,
                requestJson: "{}",
                expiresAt: DateTime.UtcNow.AddHours(1));
            job.MarkRunning();
            job.MarkCompleted("exports/101/job/result.xlsx");

            await repository.AddAsync(job, CancellationToken.None);
            await repository.SaveChangesAsync(CancellationToken.None);

            await Assert.ThrowsAsync<ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException>(
                () => exportService.GetJobDownloadAsync(job.Id, requesterUserId: 999, allowManageAll: false, CancellationToken.None));
        }
    }
}
