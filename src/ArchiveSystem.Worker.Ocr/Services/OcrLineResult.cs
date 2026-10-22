namespace ArchiveSystem.Worker.Ocr.Services
{
    public sealed record OcrLineResult
    {
        public string Text { get; init; } = string.Empty;
        public float Confidence { get; init; }
        public int Left { get; init; }
        public int Top { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
    }
}
