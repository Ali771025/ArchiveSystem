using ArchiveSystem.Application.Common.Models.AdvancedSearch;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.ValueObjects;
using ArchiveSystem.Infrastructure.Persistence;
using ArchiveSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OpenSearch.Client;
using Xunit;

namespace ArchiveSystem.Infrastructure.Tests.Services;

public sealed class OpenSearchServiceAdvancedSearchTests
{
    [Fact]
    public async Task DatabaseFallback_ReturnsOnlyFilesThatContainTheExactOcrPhrase()
    {
        var options = new DbContextOptionsBuilder<ArchiveDbContext>()
            .UseInMemoryDatabase($"AdvancedOcrSearch_{Guid.NewGuid()}")
            .Options;
        await using var dbContext = new ArchiveDbContext(options);

        var dossier = new Case(1, "CASE-1", "عبارت دقیق OCR", classificationId: 1);
        var matchingFile = CreateFile(10, dossier.Id, "matching.pdf", latestVersionId: 100);
        var unrelatedFile = CreateFile(11, dossier.Id, "unrelated.pdf", latestVersionId: 101);
        var matchingVersion = CreateVersion(100, matchingFile.Id, "matching.pdf", "پیشوند عبارت دقیق OCR پسوند");
        var unrelatedVersion = CreateVersion(101, unrelatedFile.Id, "unrelated.pdf", "متن کاملاً نامرتبط");

        dbContext.AddRange(dossier, matchingFile, unrelatedFile, matchingVersion, unrelatedVersion);
        await dbContext.SaveChangesAsync();

        var invalidResponse = new Mock<ISearchResponse<FileIndexDocument>>();
        invalidResponse.SetupGet(response => response.IsValid).Returns(false);
        invalidResponse.SetupGet(response => response.DebugInformation).Returns("OpenSearch unavailable in test");
        var client = new Mock<IOpenSearchClient>();
        client
            .Setup(search => search.SearchAsync<FileIndexDocument>(
                It.IsAny<Func<SearchDescriptor<FileIndexDocument>, ISearchRequest>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidResponse.Object);

        var service = new OpenSearchService(
            client.Object,
            NullLogger<OpenSearchService>.Instance,
            new ConfigurationBuilder().Build(),
            dbContext);

        var result = await service.AdvancedSearchAsync(new AdvancedSearchQueryDto
        {
            GeneralSearchTerm = "عبارت دقیق OCR",
            PageNumber = 1,
            PageSize = 20,
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(matchingFile.Id.ToString(), item.Id);
        Assert.Contains("عبارت دقیق ocr", item.Highlights.Single(), StringComparison.OrdinalIgnoreCase);
    }

    private static ArchiveFile CreateFile(long id, long caseId, string name, long latestVersionId)
    {
        var file = new ArchiveFile(
            id,
            name,
            FileSize.Create(100),
            FileExtension.Create(".pdf"),
            "application/pdf",
            $"files/{name}",
            caseId);
        file.ApplyLatestVersionMetadata(
            latestVersionId,
            $"files/{name}",
            100,
            "application/pdf",
            new string('a', 64));
        return file;
    }

    private static FileVersion CreateVersion(long id, long fileId, string fileName, string ocrText)
    {
        var version = new FileVersion(
            id,
            fileId,
            versionNumber: 1,
            storagePath: $"files/{fileName}",
            fileName: fileName,
            contentType: "application/pdf",
            size: 100,
            sha256: new string('a', 64));
        version.CompleteOcr("{}", ocrText, "fa", ocrText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        return version;
    }
}
