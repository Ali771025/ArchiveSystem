// src/ArchiveSystem.Infrastructure/Identity/UserCaseAccess.cs
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Infrastructure.Identity
{
    public class UserCaseAccess
    {
        public long UserId { get; set; }
        public long CaseId { get; set; }
        public string Permissions { get; set; } = string.Empty; // "Read,Write,Delete"
        public DateTime CreatedAt { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public Case Case { get; set; } = null!;
    }
}
