using System.Text;
using ArchiveSystem.Worker.Ocr.Options;
using ArchiveSystem.Worker.Ocr.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchiveSystem.Worker.Ocr.Tests.Services
{
    public class RapidOcrIdentityDocumentIntegrationTests
    {
        private const string TestImageEnvironmentVariable = "ARCHIVE_OCR_IDENTITY_TEST_IMAGE";
        private const string TestOutputEnvironmentVariable = "ARCHIVE_OCR_TEST_OUTPUT";

        [Fact]
        public async Task ExtractAsync_RecognizesRealImageAndRenderedPdfWithoutPersistingFixture()
        {
            var imagePath = Environment.GetEnvironmentVariable(TestImageEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                // Real identity data is intentionally never committed to source control.
                return;
            }

            var service = CreateService();
            await using var image = File.OpenRead(imagePath);
            var imageResult = await service.ExtractAsync(
                image,
                Path.GetFileName(imagePath),
                "image/jpeg",
                CancellationToken.None);

            AssertEngineResult(imageResult, expectedPageCount: 1);
            Assert.Equal("NationalIdCard", imageResult.DocumentType);
            Assert.Contains("identity-card-region-pass", imageResult.Warnings);
            await SaveOptionalOutputAsync(imageResult);

            var pdfPath = await CreateSingleJpegPdfAsync(imagePath);
            try
            {
                await using var pdf = File.OpenRead(pdfPath);
                var pdfResult = await service.ExtractAsync(
                    pdf,
                    "identity-document.pdf",
                    "application/pdf",
                    CancellationToken.None);

                AssertEngineResult(pdfResult, expectedPageCount: 1);
                Assert.Contains("pdf-rasterized-with-pdftoimage", pdfResult.Warnings);
            }
            finally
            {
                File.Delete(pdfPath);
            }
        }

        private static RapidOcrService CreateService() => new(
            Microsoft.Extensions.Options.Options.Create(new RapidOcrEngineOptions
            {
                ThreadCount = 4,
                ImgResize = 1400,
                TextScore = 0.50f,
                DoAngle = true,
                PdfDpi = 200,
                EnableIdentityDocumentMode = true,
                MinimumAcceptedConfidence = 0.30f
            }),
            NullLogger<RapidOcrService>.Instance);

        private static void AssertEngineResult(OcrResult result, int expectedPageCount)
        {
            Assert.True(result.HasText);
            Assert.Equal("rapidocrnet-3.0.0-paddleocr-v5-arabic-persian", result.Engine);
            Assert.Equal("fa", result.Language);
            Assert.Contains("fa", result.DetectedLanguages);
            Assert.True(result.Confidence > 0);
            Assert.Equal(expectedPageCount, result.PageCount);
            Assert.NotEmpty(result.Pages);
            Assert.Contains(result.Pages, page => page.LineElements.Count > 0);
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
                .AppendLine($"DocumentType: {result.DocumentType}")
                .AppendLine($"Confidence: {result.Confidence:0.000}")
                .AppendLine($"DocumentConfidence: {result.DocumentConfidence:0.000}")
                .AppendLine($"Warnings: {string.Join(", ", result.Warnings)}")
                .AppendLine("Fields:");

            foreach (var field in result.Fields)
            {
                output.AppendLine(
                    $"- {field.Key} = {field.NormalizedValue} | confidence={field.Confidence:0.000} | valid={field.IsValid}");
            }

            output.AppendLine("Text:").AppendLine(result.Text);
            await File.WriteAllTextAsync(outputPath, output.ToString(), new UTF8Encoding(true));
        }

        private static async Task<string> CreateSingleJpegPdfAsync(string imagePath)
        {
            var jpeg = await File.ReadAllBytesAsync(imagePath);
            var (width, height) = ReadJpegDimensions(jpeg);
            var content = Encoding.ASCII.GetBytes($"q {width} 0 0 {height} 0 0 cm /Im0 Do Q");
            var pdfPath = Path.Combine(Path.GetTempPath(), $"archive-ocr-{Guid.NewGuid():N}.pdf");

            await using var output = new FileStream(pdfPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var offsets = new List<long> { 0 };

            await WriteAsciiAsync(output, "%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
            await WriteObjectAsync(output, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
            await WriteObjectAsync(output, offsets, 2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            await WriteObjectAsync(
                output,
                offsets,
                3,
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Resources << /XObject << /Im0 4 0 R >> >> /Contents 5 0 R >>");

            offsets.Add(output.Position);
            await WriteAsciiAsync(
                output,
                $"4 0 obj\n<< /Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpeg.Length} >>\nstream\n");
            await output.WriteAsync(jpeg);
            await WriteAsciiAsync(output, "\nendstream\nendobj\n");

            offsets.Add(output.Position);
            await WriteAsciiAsync(output, $"5 0 obj\n<< /Length {content.Length} >>\nstream\n");
            await output.WriteAsync(content);
            await WriteAsciiAsync(output, "\nendstream\nendobj\n");

            var xrefOffset = output.Position;
            await WriteAsciiAsync(output, "xref\n0 6\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
            {
                await WriteAsciiAsync(output, $"{offset:0000000000} 00000 n \n");
            }

            await WriteAsciiAsync(output, $"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
            return pdfPath;
        }

        private static async Task WriteObjectAsync(
            Stream output,
            ICollection<long> offsets,
            int objectNumber,
            string body)
        {
            offsets.Add(output.Position);
            await WriteAsciiAsync(output, $"{objectNumber} 0 obj\n{body}\nendobj\n");
        }

        private static Task WriteAsciiAsync(Stream output, string value) =>
            output.WriteAsync(Encoding.Latin1.GetBytes(value)).AsTask();

        private static (int Width, int Height) ReadJpegDimensions(byte[] jpeg)
        {
            if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
            {
                throw new InvalidDataException("Identity OCR integration fixture must be a JPEG image.");
            }

            var index = 2;
            while (index + 8 < jpeg.Length)
            {
                if (jpeg[index] != 0xFF)
                {
                    index++;
                    continue;
                }

                var marker = jpeg[index + 1];
                index += 2;
                if (marker is 0xD8 or 0xD9)
                {
                    continue;
                }

                var length = (jpeg[index] << 8) + jpeg[index + 1];
                if (length < 2 || index + length > jpeg.Length)
                {
                    break;
                }

                if (marker is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF)
                {
                    var height = (jpeg[index + 3] << 8) + jpeg[index + 4];
                    var width = (jpeg[index + 5] << 8) + jpeg[index + 6];
                    return (width, height);
                }

                index += length;
            }

            throw new InvalidDataException("Could not read JPEG dimensions.");
        }
    }
}
