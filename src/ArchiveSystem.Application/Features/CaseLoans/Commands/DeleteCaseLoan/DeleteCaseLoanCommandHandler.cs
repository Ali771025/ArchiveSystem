using ArchiveSystem.Application.Features.CaseLoans.Interfaces;
using ArchiveSystem.Application.Common.Exceptions;
using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Commands.DeleteCaseLoan
{
    public sealed class DeleteCaseLoanCommandHandler : IRequestHandler<DeleteCaseLoanCommand, bool>
    {
        private readonly ICaseLoanRepository _caseLoanRepository;

        public DeleteCaseLoanCommandHandler(ICaseLoanRepository caseLoanRepository)
        {
            _caseLoanRepository = caseLoanRepository;
        }

        public async Task<bool> Handle(DeleteCaseLoanCommand request, CancellationToken cancellationToken)
        {
            var caseLoan = await _caseLoanRepository.GetByIdAsync(request.Id, cancellationToken);
            if (caseLoan is null)
            {
                return false;
            }

            if (caseLoan.IsClosed)
            {
                throw new ConflictException(
                    "سابقه امانت برگشت‌داده‌شده مختومه است و امکان حذف آن وجود ندارد.");
            }

            _caseLoanRepository.Remove(caseLoan);
            await _caseLoanRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
