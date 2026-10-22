namespace ArchiveSystem.Worker.Ocr.Options
{
    public class OcrWorkerOptions
    {
        public const string SectionName = "OcrWorker";

        public int PollIntervalSeconds { get; set; } = 15;
        public int BatchSize { get; set; } = 50;
        public int MaxConcurrentJobs { get; set; } = 2;
        public int JobTimeoutMinutes { get; set; } = 10;
        public int StaleProcessingTimeoutMinutes { get; set; } = 30;
        public int StaleProcessingResetIntervalMinutes { get; set; } = 5;
    }
}
