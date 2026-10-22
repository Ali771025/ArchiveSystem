namespace ArchiveSystem.Application.Features.CaseLoans.DTOs
{
    public sealed record CaseLoanListItemDto(
        Guid Id,
        string BorrowerFirstName,
        string BorrowerLastName,
        string BorrowerFullName,
        string CaseNumber,
        string NationalId,
        string ContactNumber,
        DateTime CheckoutDate,
        DateTime? ExpectedReturnDate,
        DateTime? ActualReturnDate,
        string Status,
        string? ReceiverUnit,
        string? RegisteredBy,
        bool IsReturned,
        string? Description,
        DateTime CreatedAt,
        DateTime? ModifiedAt);

    public sealed record CaseLoanDetailsDto(
        Guid Id,
        string BorrowerFirstName,
        string BorrowerLastName,
        string CaseNumber,
        string NationalId,
        string ContactNumber,
        DateTime CheckoutDate,
        DateTime? ExpectedReturnDate,
        DateTime? ActualReturnDate,
        string Status,
        string? ReceiverUnit,
        string? RegisteredBy,
        string? Description,
        bool IsReturned,
        DateTime CreatedAt,
        DateTime? ModifiedAt);
}
