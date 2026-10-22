using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Features.CaseLoans.Interfaces
{
    public interface ICaseLoanRepository
    {
        Task<PagedResponse<CaseLoanListItemDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? caseNumber,
            string? nationalId,
            string? borrowerLastName,
            string? columnFilterField,
            string? columnFilterOperator,
            string? columnFilterValue,
            CancellationToken cancellationToken = default);

        Task<CaseLoanDetailsDto?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CaseLoan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<CaseLoan?> GetLatestByCaseNumberAsync(
            string caseNumber,
            CancellationToken cancellationToken = default);
        Task AddAsync(CaseLoan caseLoan, CancellationToken cancellationToken = default);
        void Remove(CaseLoan caseLoan);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
