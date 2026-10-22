namespace ArchiveSystem.Domain.Entities
{
    public class Permission : BaseEntity<long>, IAggregateRoot
    {
        public string Key { get; private set; }
        public string Description { get; private set; }

        private Permission() : base()
        {
            Key = string.Empty;
            Description = string.Empty;
        }

        public Permission(long id, string key, string description = "") : base(id)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Permission key is required.", nameof(key));
            }

            Key = key.Trim();
            Description = description?.Trim() ?? string.Empty;
        }
    }
}
