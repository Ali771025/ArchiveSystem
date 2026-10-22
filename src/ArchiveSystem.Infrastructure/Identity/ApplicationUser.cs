// src/ArchiveSystem.Infrastructure/Identity/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<long>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? PersonnelCode { get; set; }
        public TimeSpan ActiveDuration { get; set; } = TimeSpan.Zero;
        public bool AutoBackupEnabled { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? ModifiedAt
        {
            get => LastModified;
            set => LastModified = value;
        }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastPasswordResetRequestedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<UserCaseAccess> UserCaseAccesses { get; set; } = new List<UserCaseAccess>();
    }
}
