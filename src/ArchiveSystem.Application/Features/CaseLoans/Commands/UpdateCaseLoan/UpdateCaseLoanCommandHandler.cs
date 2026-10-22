using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.CaseLoans.Interfaces;
using ArchiveSystem.Application.Common.Exceptions;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Commands.UpdateCaseLoan
{
    public sealed class UpdateCaseLoanCommandHandler : IRequestHandler<UpdateCaseLoanCommand, CaseLoanDetailsDto?>
    {
        private readonly ICaseLoanRepository _caseLoanRepository;

        public UpdateCaseLoanCommandHandler(ICaseLoanRepository caseLoanRepository)
        {
            _caseLoanRepository = caseLoanRepository;
        }

        public async Task<CaseLoanDetailsDto?> Handle(UpdateCaseLoanCommand request, CancellationToken cancellationToken)
        {
            var caseLoan = await _caseLoanRepository.GetByIdAsync(request.Id, cancellationToken);
            if (caseLoan is null)
            {
                return null;
            }

            if (caseLoan.IsClosed)
            {
                throw new ConflictException(
                    "سابقه امانت برگشت‌داده‌شده مختومه است و امکان ویرایش آن وجود ندارد.");
            }

            var latestLoan = await _caseLoanRepository.GetLatestByCaseNumberAsync(
                request.CaseNumber,
                cancellationToken);
            if (latestLoan is not null &&
                latestLoan.Id != caseLoan.Id &&
                !latestLoan.IsClosed)
            {
                throw new ConflictException(
                    $"پرونده «{request.CaseNumber.Trim()}» دارای امانت فعال است و امکان ثبت امانت هم‌زمان ندارد.");
            }

            caseLoan.Update(
                request.BorrowerFirstName,
                request.BorrowerLastName,
                request.CaseNumber,
                request.NationalId.Trim(),
                request.ContactNumber,
                request.CheckoutDate,
                request.ExpectedReturnDate,
                request.ActualReturnDate,
                request.Description,
                request.Status,
                request.ReceiverUnit,
                request.RegisteredBy,
                request.IsReturned);

            await _caseLoanRepository.SaveChangesAsync(cancellationToken);

            return new CaseLoanDetailsDto(
                caseLoan.Id,
                caseLoan.BorrowerFirstName,
                caseLoan.BorrowerLastName,
                caseLoan.CaseNumber,
                caseLoan.NationalId,
                caseLoan.ContactNumber,
                caseLoan.CheckoutDate,
                caseLoan.ExpectedReturnDate,
                caseLoan.ActualReturnDate,
                caseLoan.Status,
                caseLoan.ReceiverUnit,
                caseLoan.RegisteredBy,
                caseLoan.Description,
                caseLoan.IsReturned,
                caseLoan.CreatedAt,
                caseLoan.ModifiedAt);
        }
    }
}
