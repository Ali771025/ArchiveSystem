using System.ComponentModel.DataAnnotations;

namespace ArchiveSystem.Worker.Thumbnail.Options
{
    public sealed class ThumbnailOptions
    {
        public const string SectionName = "Thumbnail";

        [Range(1, 3600)]
        public int PollIntervalSeconds { get; set; } = 15;

        [Range(1, 1000)]
        public int BatchSize { get; set; } = 100;

        [Range(1, 64)]
        public int MaxConcurrency { get; set; } = 4;

        [Range(32, 4096)]
        public int MaxDimension { get; set; } = 256;

        [Required]
        public string OutputFormat { get; set; } = "jpeg";

        [Range(1, 100)]
        public int JpegQuality { get; set; } = 75;

        [Range(1, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        [Range(50, 60000)]
        public int RetryDelayMilliseconds { get; set; } = 250;
    }
}
