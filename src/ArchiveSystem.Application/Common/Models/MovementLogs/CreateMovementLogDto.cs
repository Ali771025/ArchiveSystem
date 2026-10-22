namespace ArchiveSystem.Application.Common.Models.MovementLogs
{
    public sealed class CreateMovementLogDto
    {
        public string DossierNumber { get; set; } = string.Empty;
        public string ReceiverFirstName { get; set; } = string.Empty;
        public string ReceiverLastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CheckoutDate { get; set; }
        public DateTime? ExpectedReturnDate { get; set; }
        public string? Description { get; set; }
    }
}
