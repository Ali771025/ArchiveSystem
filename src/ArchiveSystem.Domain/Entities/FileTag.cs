namespace ArchiveSystem.Domain.Entities
{
    public class FileTag
    {
        public long FileId { get; set; }
        public long TagId { get; set; }

        public ArchiveFile File { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}
