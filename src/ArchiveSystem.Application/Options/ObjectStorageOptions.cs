namespace ArchiveSystem.Application.Options
{
    public class ObjectStorageOptions
    {
        public const string SectionName = "MinIO";

        public string BucketName { get; set; } = "archive-system";
    }
}
