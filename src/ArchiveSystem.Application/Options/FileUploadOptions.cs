namespace ArchiveSystem.Application.Options
{
    public class FileUploadOptions
    {
        public const string SectionName = "FileUpload";

        public long MaxFileSizeBytes { get; set; } = 100L * 1024 * 1024;
        public int DefaultPartSizeBytes { get; set; } = 8 * 1024 * 1024;
        public int MinPartSizeBytes { get; set; } = 5 * 1024 * 1024;
        public int MaxPartSizeBytes { get; set; } = 64 * 1024 * 1024;
        public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();
        public string[] BlockedExtensions { get; set; } =
        {
            ".exe",
            ".bat",
            ".cmd",
            ".com",
            ".msi",
            ".ps1",
            ".php",
            ".js",
            ".vbs",
            ".scr"
        };
        public string DefaultTenantId { get; set; } = "default";
    }
}
