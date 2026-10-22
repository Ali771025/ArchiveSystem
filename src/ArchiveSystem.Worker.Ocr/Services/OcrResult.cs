// src/ArchiveSystem.Worker.Ocr/Services/OcrResult.cs
namespace ArchiveSystem.Worker.Ocr.Services
{
    public sealed record OcrResult
    {
        public string Engine { get; init; } = "dummy-advanced";
        public string Language { get; init; } = "fa";
        public float Confidence { get; init; }
        public int PageCount { get; init; }
        public int WordCount { get; init; }
        public long ProcessingDurationMs { get; init; }
        public bool HasText { get; init; }
        public string LayoutProfile { get; init; } = "unknown";
        public string Template { get; init; } = "Generic";
        public string DocumentType { get; init; } = "Generic";
        public float DocumentConfidence { get; init; }
        public string Title { get; init; } = string.Empty;
        public string ReferenceCode { get; init; } = string.Empty;
        public IReadOnlyList<string> DetectedLanguages { get; init; } = Array.Empty<string>();
        public IReadOnlyList<OcrPageResult> Pages { get; init; } = Array.Empty<OcrPageResult>();
        public IReadOnlyList<OcrFieldResult> Fields { get; init; } = Array.Empty<OcrFieldResult>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public string Text { get; init; } = string.Empty;
    }
}
