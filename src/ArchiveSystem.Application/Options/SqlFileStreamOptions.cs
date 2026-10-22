namespace ArchiveSystem.Application.Options
{
    public class SqlFileStreamOptions
    {
        public const string SectionName = "SqlFileStream";

        public string ContainerPath { get; set; } = "tenant/default/files";
    }
}
