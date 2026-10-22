using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Domain.Entities;

namespace ArchiveSystem.Application.Features.Taxpayers.Interfaces
{
    public interface ITaxpayerRepository
    {
        Task<PagedResponse<TaxpayerListItemDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            string? firstName,
            string? lastName,
            string? nationalId,
            string? columnFilterField,
            string? columnFilterOperator,
            string? columnFilterValue,
            CancellationToken cancellationToken = default);

        Task<TaxpayerDetailsDto?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Taxpayer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNationalIdAsync(string nationalId, Guid? excludingId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByNationalCompanyIdAsync(string nationalCompanyId, Guid? excludingId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsByMobileAsync(string mobile, Guid? excludingId = null, CancellationToken cancellationToken = default);
        Task AddAsync(Taxpayer taxpayer, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
