using System.Text;
using ArchiveSystem.Worker.Ocr.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchiveSystem.Worker.Ocr.Tests.Services
{
    public class DummyOcrServiceTests
    {
        [Fact]
        public async Task ExtractAsync_ReturnsFarsiStructuredResult()
        {
            var service = new DummyOcrService(NullLogger<DummyOcrService>.Instance);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("نمونه متن براي تست OCR فارسي"));

            var result = await service.ExtractAsync(stream, "archive-fa-sample.pdf", "application/pdf", CancellationToken.None);

            Assert.Equal("dummy-advanced-fa", result.Engine);
            Assert.Equal("fa", result.Language);
            Assert.True(result.HasText);
            Assert.True(result.WordCount > 0);
            Assert.True(result.PageCount >= 1);
            Assert.NotEmpty(result.Text);
            Assert.NotEmpty(result.Pages);
            Assert.Contains("fa", result.DetectedLanguages);
            Assert.Contains("یادداشت OCR", result.Text);
        }

        [Fact]
        public async Task ExtractAsync_IsDeterministicForSameInput()
        {
            var service = new DummyOcrService(NullLogger<DummyOcrService>.Instance);
            var payload = Encoding.UTF8.GetBytes("سند آرشیوی تست برای بررسی پایداری خروجی");

            await using var stream1 = new MemoryStream(payload);
            await using var stream2 = new MemoryStream(payload);

            var result1 = await service.ExtractAsync(stream1, "record-1403.pdf", "application/pdf", CancellationToken.None);
            var result2 = await service.ExtractAsync(stream2, "record-1403.pdf", "application/pdf", CancellationToken.None);

            Assert.Equal(result1.Text, result2.Text);
            Assert.Equal(result1.ReferenceCode, result2.ReferenceCode);
            Assert.Equal(result1.PageCount, result2.PageCount);
            Assert.Equal(result1.Template, result2.Template);
        }
    }
}
