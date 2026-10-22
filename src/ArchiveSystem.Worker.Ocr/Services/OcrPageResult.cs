// src/ArchiveSystem.Worker.Ocr/Services/OcrPageResult.cs
namespace ArchiveSystem.Worker.Ocr.Services
{
    public sealed class OcrPageResult
    {
        public int PageNumber { get; init; }
        public float Confidence { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public string Text { get; init; } = string.Empty;
        public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
        public IReadOnlyList<OcrLineResult> LineElements { get; init; } = Array.Empty<OcrLineResult>();
    }
}
