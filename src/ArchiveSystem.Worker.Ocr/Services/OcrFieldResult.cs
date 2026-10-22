namespace ArchiveSystem.Worker.Ocr.Services
{
    public sealed record OcrFieldResult
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string NormalizedValue { get; init; } = string.Empty;
        public float Confidence { get; init; }
        public bool IsValid { get; init; }
        public int PageNumber { get; init; } = 1;
        public string? ValidationMessage { get; init; }
    }
}
