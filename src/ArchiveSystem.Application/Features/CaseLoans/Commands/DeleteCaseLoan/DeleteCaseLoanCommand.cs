using MediatR;

namespace ArchiveSystem.Application.Features.CaseLoans.Commands.DeleteCaseLoan
{
    public sealed record DeleteCaseLoanCommand(Guid Id) : IRequest<bool>;
}
