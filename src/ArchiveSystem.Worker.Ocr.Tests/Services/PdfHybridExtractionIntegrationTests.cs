using System.Text;
using ArchiveSystem.Worker.Ocr.Options;
using ArchiveSystem.Worker.Ocr.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ArchiveSystem.Worker.Ocr.Tests.Services
{
    public class PdfHybridExtractionIntegrationTests
    {
        private const string TestPdfEnvironmentVariable = "ARCHIVE_OCR_HYBRID_PDF_TEST_FILE";
        private const string TestOutputEnvironmentVariable = "ARCHIVE_OCR_HYBRID_PDF_TEST_OUTPUT";

        [Fact]
        public async Task ExtractAsync_UsesNativeTextWithoutOcrForDigitalPdf()
        {
            var pdfPath = Environment.GetEnvironmentVariable(TestPdfEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            {
                return;
            }

            var service = new RapidOcrService(
                Microsoft.Extensions.Options.Options.Create(new RapidOcrEngineOptions
                {
                    EnablePdfNativeTextExtraction = true,
                    PdfNativeTextMinimumCharacters = 40,
                    OcrPdfImagesWhenNativeTextExists = true,
                    PdfImageOcrMinimumCoverage = 0.08f,
                    EnableIdentityDocumentMode = false
                }),
                NullLogger<RapidOcrService>.Instance);

            await using var pdf = File.OpenRead(pdfPath);
            var result = await service.ExtractAsync(
                pdf,
                Path.GetFileName(pdfPath),
                "application/pdf",
                CancellationToken.None);

            Assert.Equal("pdfpig-0.1.15-native-text", result.Engine);
            Assert.Equal(15, result.PageCount);
            Assert.Contains("pdf-native-text-extracted", result.Warnings);
            Assert.DoesNotContain("pdf-rasterized-with-pdftoimage", result.Warnings);
            Assert.DoesNotContain("pdf-hybrid-native-text-and-ocr", result.Warnings);
            Assert.Contains("پاسخ فنی به سند درخواست پیشنهاد", result.Text);
            Assert.Contains("سامانه هوشمند گزارش ساز", result.Text);

            await SaveOptionalOutputAsync(result);
        }

        private static async Task SaveOptionalOutputAsync(OcrResult result)
        {
            var outputPath = Environment.GetEnvironmentVariable(TestOutputEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var output = new StringBuilder()
                .AppendLine($"Engine: {result.Engine}")
                .AppendLine($"Pages: {result.PageCount}")
                .AppendLine($"Words: {result.WordCount}")
                .AppendLine($"DurationMs: {result.ProcessingDurationMs}")
                .AppendLine($"Warnings: {string.Join(", ", result.Warnings)}");

            foreach (var page in result.Pages)
            {
                output.AppendLine().AppendLine($"===== PAGE {page.PageNumber} =====");
                output.AppendLine(page.Text);
            }

            await File.WriteAllTextAsync(outputPath, output.ToString(), new UTF8Encoding(true));
        }
    }
}
