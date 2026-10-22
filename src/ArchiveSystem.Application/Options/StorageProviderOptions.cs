namespace ArchiveSystem.Application.Options
{
    public class StorageProviderOptions
    {
        public const string SectionName = "Storage";

        public string Provider { get; set; } = "MinIO";
    }
}
