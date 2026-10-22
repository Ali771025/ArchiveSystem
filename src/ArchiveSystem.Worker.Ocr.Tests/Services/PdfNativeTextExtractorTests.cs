using System.Text;
using ArchiveSystem.Worker.Ocr.Services;
using Xunit;

namespace ArchiveSystem.Worker.Ocr.Tests.Services
{
    public class PdfNativeTextExtractorTests
    {
        [Fact]
        public async Task Extract_ReadsDigitalTextAndDoesNotMarkPageAsImage()
        {
            var pdfPath = await CreateTextPdfAsync("Hello native PDF text");
            try
            {
                var pages = PdfNativeTextExtractor.Extract(pdfPath, 5, 0.08f);

                var page = Assert.Single(pages);
                Assert.True(page.HasMeaningfulText);
                Assert.False(page.HasSignificantImage);
                Assert.Equal(0, page.ImageCoverage);
                Assert.Contains("Hello native PDF text", page.Text);
            }
            finally
            {
                File.Delete(pdfPath);
            }
        }

        private static async Task<string> CreateTextPdfAsync(string text)
        {
            var escaped = text.Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
            var content = Encoding.ASCII.GetBytes($"BT /F1 18 Tf 50 700 Td ({escaped}) Tj ET");
            var path = Path.Combine(Path.GetTempPath(), $"archive-native-pdf-{Guid.NewGuid():N}.pdf");
            await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var offsets = new List<long> { 0 };

            await WriteAsciiAsync(output, "%PDF-1.4\n");
            await WriteObjectAsync(output, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
            await WriteObjectAsync(output, offsets, 2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            await WriteObjectAsync(
                output,
                offsets,
                3,
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>");
            await WriteObjectAsync(output, offsets, 4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

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
            return path;
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
            output.WriteAsync(Encoding.ASCII.GetBytes(value)).AsTask();
    }
}
