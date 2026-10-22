using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Commands.CreateCaseLoan
{
    public sealed record CreateCaseLoanCommand(
        string BorrowerFirstName,
        string BorrowerLastName,
        string CaseNumber,
        string NationalId,
        string ContactNumber,
        DateTime CheckoutDate,
        DateTime? ExpectedReturnDate,
        DateTime? ActualReturnDate,
        string? Status,
        string? ReceiverUnit,
        string? RegisteredBy,
        string? Description,
        bool IsReturned = false) : IRequest<CaseLoanDetailsDto>;
}
