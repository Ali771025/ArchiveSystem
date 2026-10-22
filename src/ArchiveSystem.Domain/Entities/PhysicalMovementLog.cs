using System.ComponentModel.DataAnnotations;

namespace ArchiveSystem.Domain.Entities
{
    public class PhysicalMovementLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string DossierNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ReceiverFirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ReceiverLastName { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [RegularExpression(@"^\d{10}$")]
        public string NationalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime CheckoutDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
