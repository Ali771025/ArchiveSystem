namespace ArchiveSystem.Domain.Entities
{
    public class UserRole
    {
        public long UserId { get; set; }
        public long RoleId { get; set; }

        public Role Role { get; set; } = null!;
    }
}
