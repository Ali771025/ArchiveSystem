using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace ArchiveSystem.Worker.Ocr.Services
{
    public static class PdfNativeTextExtractor
    {
        public static IReadOnlyList<PdfNativePageContent> Extract(
            string pdfPath,
            int minimumTextCharacters,
            float imageOcrMinimumCoverage)
        {
            using var document = PdfDocument.Open(pdfPath);
            var pages = new List<PdfNativePageContent>(document.NumberOfPages);

            foreach (var page in document.GetPages())
            {
                var rawText = ContentOrderTextExtractor.GetText(page);
                var lines = RestoreLogicalOrder(rawText);
                var text = string.Join(Environment.NewLine, lines);
                var meaningfulCharacters = text.Count(character => !char.IsWhiteSpace(character));
                var pageArea = Math.Max(1d, Math.Abs((double)page.Width * (double)page.Height));
                var imageCoverage = Math.Clamp(
                    page.GetImages().Sum(image =>
                        Math.Abs((double)image.BoundingBox.Width * (double)image.BoundingBox.Height)) / pageArea,
                    0d,
                    1d);

                pages.Add(new PdfNativePageContent(
                    page.Number,
                    (int)Math.Ceiling((double)page.Width),
                    (int)Math.Ceiling((double)page.Height),
                    text,
                    lines,
                    meaningfulCharacters >= Math.Max(1, minimumTextCharacters),
                    imageCoverage,
                    imageCoverage >= Math.Clamp(imageOcrMinimumCoverage, 0f, 1f)));
            }

            return pages;
        }

        private static IReadOnlyList<string> RestoreLogicalOrder(string? rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return Array.Empty<string>();
            }

            return rawText
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(PaddleArabicTextDecoder.RestoreLogicalOrder)
                .Select(line => PersianOcrText.Normalize(line, false))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }
    }

    public sealed record PdfNativePageContent(
        int PageNumber,
        int Width,
        int Height,
        string Text,
        IReadOnlyList<string> Lines,
        bool HasMeaningfulText,
        double ImageCoverage,
        bool HasSignificantImage);
}
