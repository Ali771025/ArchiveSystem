// src/ArchiveSystem.Infrastructure/Configurations/StorageOptions.cs
namespace ArchiveSystem.Infrastructure.Configurations
{
    public class StorageOptions
    {
        public const string SectionName = "Storage";

        public string Endpoint { get; set; } = "http://localhost:9000";
        public string AccessKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string BucketName { get; set; } = "archive-system";
        public int MaxUploadSizeMb { get; set; } = 100;
        public int UploadTimeoutSeconds { get; set; } = 300;
    }
}