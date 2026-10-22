namespace ArchiveSystem.Worker.ScannerIngest.Options
{
    public class ScannerIngestOptions
    {
        public const string SectionName = "ScannerIngest";

        public bool Enabled { get; set; } = false;
        public string WatchPath { get; set; } = string.Empty;
        public string ProcessedPath { get; set; } = string.Empty;
        public string ErrorPath { get; set; } = string.Empty;
        public int PollIntervalSeconds { get; set; } = 10;
        public int StableSeconds { get; set; } = 5;
        public string[] AllowedExtensions { get; set; } = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".tif", ".tiff" };
        public long? DefaultClassificationId { get; set; }
        public long? DefaultCaseId { get; set; }
    }
}
