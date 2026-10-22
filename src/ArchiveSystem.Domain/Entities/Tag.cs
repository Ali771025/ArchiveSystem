namespace ArchiveSystem.Domain.Entities
{
    public class Tag : BaseEntity<long>, IAggregateRoot
    {
        public string Name { get; private set; }
        public string NormalizedName { get; private set; }
        public string? Description { get; private set; }

        private Tag() : base()
        {
            Name = string.Empty;
            NormalizedName = string.Empty;
        }

        public Tag(long id, string name, string? description = null) : base(id)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tag name is required.", nameof(name));
            }

            Name = Common.PersianTextNormalizer.CleanTagName(name);
            NormalizedName = Common.PersianTextNormalizer.NormalizeTagName(name);
            Description = description?.Trim();
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tag name is required.", nameof(name));
            }

            Name = Common.PersianTextNormalizer.CleanTagName(name);
            NormalizedName = Common.PersianTextNormalizer.NormalizeTagName(name);
            MarkAsModified();
        }
    }
}
