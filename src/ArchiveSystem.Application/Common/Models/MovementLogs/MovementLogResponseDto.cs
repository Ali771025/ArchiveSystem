namespace ArchiveSystem.Application.Common.Models.MovementLogs
{
    public sealed class MovementLogResponseDto
    {
        public Guid Id { get; set; }
        public string DossierNumber { get; set; } = string.Empty;
        public string ReceiverFirstName { get; set; } = string.Empty;
        public string ReceiverLastName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CheckoutDate { get; set; } = string.Empty;
        public string? ExpectedReturnDate { get; set; }
        public string? Description { get; set; }
        public string CreatedAtUtc { get; set; } = string.Empty;
    }
}
