using System.Text.Json;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Interfaces.Storage;
using ArchiveSystem.Application.Options;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Events;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Worker.Ocr.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ArchiveSystem.Worker.Ocr.Tests.Services
{
    public class OcrProcessingServiceTests
    {
        [Fact]
        public async Task ProcessFileVersionAsync_PersistsStableIndentedCamelCasePayload()
        {
            var repository = new Mock<IFileVersionRepository>();
            var storage = new Mock<IObjectStorage>();
            var bus = new Mock<IMessageBus>();
            var ocrService = new Mock<IOcrService>();

            var version = CreateVersion();

            repository.Setup(x => x.TryStartOcrProcessingAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            repository.Setup(x => x.GetByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
            storage.Setup(x => x.ExistsAsync("archive-system", version.StoragePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            storage.Setup(x => x.GetObjectStreamAsync("archive-system", version.StoragePath, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

            var ocrResult = new OcrResult
            {
                Engine = "dummy-advanced-fa",
                Language = "fa",
                Confidence = 0.91f,
                WordCount = 120,
                PageCount = 2,
                HasText = true,
                LayoutProfile = "multi-page-rtl",
                Template = "Archive",
                Title = "سند آرشیوی",
                ReferenceCode = "OCR-FA-123456",
                DetectedLanguages = new[] { "fa", "en" },
                ProcessingDurationMs = 777,
                Warnings = new[] { "dummy-engine-output" },
                Text = "متن شبیه سازی شده",
                Pages = new[]
                {
                    new OcrPageResult
                    {
                        PageNumber = 1,
                        Confidence = 0.90f,
                        Text = "صفحه اول",
                        Lines = new[] { "خط اول", "خط دوم" }
                    }
                }
            };

            ocrService.Setup(x => x.ExtractAsync(It.IsAny<Stream>(), version.FileName, version.ContentType, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ocrResult);

            string? persistedPayload = null;
            repository.Setup(x => x.TryCompleteOcrAsync(version.Id, It.IsAny<string>(), It.IsAny<string>(), "fa", 120, It.IsAny<CancellationToken>()))
                .Callback<long, string, string, int, CancellationToken>((_, payload, _, _, _) => persistedPayload = payload)
                .ReturnsAsync(true);

            var service = CreateService(repository, storage, ocrService, bus);

            await service.ProcessFileVersionAsync(version.Id, CancellationToken.None);

            Assert.NotNull(persistedPayload);
            Assert.Contains("\n  \"engine\"", persistedPayload);
            Assert.Contains("processingDurationMs", persistedPayload);
            Assert.DoesNotContain("ProcessingDurationMs", persistedPayload);

            using var doc = JsonDocument.Parse(persistedPayload!);
            var root = doc.RootElement;
            Assert.Equal("dummy-advanced-fa", root.GetProperty("engine").GetString());
            Assert.Equal("fa", root.GetProperty("language").GetString());
            Assert.Equal(777, root.GetProperty("processingDurationMs").GetInt64());
            Assert.Equal("Archive", root.GetProperty("template").GetString());
            Assert.True(root.GetProperty("pages").GetArrayLength() > 0);

            bus.Verify(x => x.PublishAsync(It.IsAny<OcrCompletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessFileVersionAsync_FillsFallbackDurationWhenMissingFromResult()
        {
            var repository = new Mock<IFileVersionRepository>();
            var storage = new Mock<IObjectStorage>();
            var bus = new Mock<IMessageBus>();
            var ocrService = new Mock<IOcrService>();

            var version = CreateVersion();

            repository.Setup(x => x.TryStartOcrProcessingAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            repository.Setup(x => x.GetByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
            storage.Setup(x => x.ExistsAsync("archive-system", version.StoragePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            storage.Setup(x => x.GetObjectStreamAsync("archive-system", version.StoragePath, It.IsAny<CancellationToken>())).ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

            ocrService.Setup(x => x.ExtractAsync(It.IsAny<Stream>(), version.FileName, version.ContentType, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(25);
                    return new OcrResult
                    {
                        Engine = "dummy-advanced-fa",
                        Language = "fa",
                        Confidence = 0.91f,
                        WordCount = 20,
                        PageCount = 1,
                        HasText = true,
                        LayoutProfile = "single-page-rtl",
                        Template = "Generic",
                        Title = "سند تست",
                        ReferenceCode = "OCR-FA-000001",
                        DetectedLanguages = new[] { "fa" },
                        ProcessingDurationMs = 0,
                        Warnings = Array.Empty<string>(),
                        Text = "متن تست",
                        Pages = new[]
                        {
                            new OcrPageResult
                            {
                                PageNumber = 1,
                                Confidence = 0.91f,
                                Text = "متن تست",
                                Lines = new[] { "متن تست" }
                            }
                        }
                    };
                });

            string? persistedPayload = null;
            repository.Setup(x => x.TryCompleteOcrAsync(version.Id, It.IsAny<string>(), It.IsAny<string>(), "fa", 20, It.IsAny<CancellationToken>()))
                .Callback<long, string, string, int, CancellationToken>((_, payload, _, _, _) => persistedPayload = payload)
                .ReturnsAsync(true);

            var service = CreateService(repository, storage, ocrService, bus);

            await service.ProcessFileVersionAsync(version.Id, CancellationToken.None);

            Assert.NotNull(persistedPayload);

            using var doc = JsonDocument.Parse(persistedPayload!);
            var duration = doc.RootElement.GetProperty("processingDurationMs").GetInt64();
            Assert.True(duration > 0);
        }

        [Fact]
        public async Task ProcessFileVersionAsync_PersistsEngineFailureMessage()
        {
            const string failureMessage = "RapidOCR recognition model could not be loaded.";
            var repository = new Mock<IFileVersionRepository>();
            var storage = new Mock<IObjectStorage>();
            var bus = new Mock<IMessageBus>();
            var ocrService = new Mock<IOcrService>();
            var version = CreateVersion();

            repository.Setup(x => x.TryStartOcrProcessingAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            repository.Setup(x => x.GetByIdAsync(version.Id, It.IsAny<CancellationToken>())).ReturnsAsync(version);
            repository.Setup(x => x.TryFailOcrAsync(version.Id, It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            storage.Setup(x => x.ExistsAsync("archive-system", version.StoragePath, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            storage.Setup(x => x.GetObjectStreamAsync("archive-system", version.StoragePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));
            ocrService.Setup(x => x.ExtractAsync(It.IsAny<Stream>(), version.FileName, version.ContentType, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(failureMessage));

            string? persistedError = null;
            repository.Setup(x => x.TryFailOcrAsync(version.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<long, string, CancellationToken>((_, error, _) => persistedError = error)
                .ReturnsAsync(true);

            var service = CreateService(repository, storage, ocrService, bus);
            await service.ProcessFileVersionAsync(version.Id, CancellationToken.None);

            Assert.NotNull(persistedError);
            Assert.Equal(failureMessage, persistedError);
        }

        private static FileVersion CreateVersion()
        {
            return new FileVersion(
                id: 55,
                fileId: 12,
                versionNumber: 1,
                storagePath: "ocr/archive-fa.pdf",
                fileName: "archive-fa.pdf",
                contentType: "application/pdf",
                size: 1024,
                sha256: new string('a', 64));
        }

        private static OcrProcessingService CreateService(
            Mock<IFileVersionRepository> repository,
            Mock<IObjectStorage> storage,
            Mock<IOcrService> ocrService,
            Mock<IMessageBus> bus)
        {
            return new OcrProcessingService(
                repository.Object,
                storage.Object,
                Microsoft.Extensions.Options.Options.Create(new ObjectStorageOptions { BucketName = "archive-system" }),
                Microsoft.Extensions.Options.Options.Create(new ArchiveSystem.Worker.Ocr.Options.OcrWorkerOptions
                {
                    JobTimeoutMinutes = 0
                }),
                ocrService.Object,
                bus.Object,
                NullLogger<OcrProcessingService>.Instance);
        }
    }
}
