using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.CaseLoans.Interfaces;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Domain.Entities;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Commands.CreateCaseLoan
{
    public sealed class CreateCaseLoanCommandHandler : IRequestHandler<CreateCaseLoanCommand, CaseLoanDetailsDto>
    {
        private readonly ICaseLoanRepository _caseLoanRepository;

        public CreateCaseLoanCommandHandler(ICaseLoanRepository caseLoanRepository)
        {
            _caseLoanRepository = caseLoanRepository;
        }

        public async Task<CaseLoanDetailsDto> Handle(CreateCaseLoanCommand request, CancellationToken cancellationToken)
        {
            var latestLoan = await _caseLoanRepository.GetLatestByCaseNumberAsync(
                request.CaseNumber,
                cancellationToken);
            if (latestLoan is not null && !latestLoan.IsClosed)
            {
                throw new ConflictException(
                    $"پرونده «{request.CaseNumber.Trim()}» هنوز در وضعیت امانت یا دیرکرد است و تا ثبت بازگشت واقعی، امکان امانت مجدد ندارد.");
            }

            var caseLoan = new CaseLoan(
                Guid.NewGuid(),
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

            await _caseLoanRepository.AddAsync(caseLoan, cancellationToken);
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
