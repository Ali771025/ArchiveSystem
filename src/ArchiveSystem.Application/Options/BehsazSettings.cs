namespace ArchiveSystem.Application.Options
{
    public sealed class BehsazSettings
    {
        public const string SectionName = "BehsazIntegration";

        public bool IsEnabled { get; set; }
        public int UserCode { get; set; } = 1000;
        public string ConnectionString { get; set; } = string.Empty;
    }
}
