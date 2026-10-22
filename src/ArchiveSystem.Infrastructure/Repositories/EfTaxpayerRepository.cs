using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.Taxpayers.Interfaces;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public sealed class EfTaxpayerRepository : ITaxpayerRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfTaxpayerRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResponse<TaxpayerListItemDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            string? firstName,
            string? lastName,
            string? nationalId,
            string? columnFilterField,
            string? columnFilterOperator,
            string? columnFilterValue,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Taxpayers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(x =>
                    ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Contains(normalized) ||
                    ((x.LastName ?? "") + " " + (x.FirstName ?? "")).Contains(normalized) ||
                    (x.CompanyName != null && x.CompanyName.Contains(normalized)) ||
                    (x.NationalId != null && x.NationalId.Contains(normalized)) ||
                    (x.NationalCompanyId != null && x.NationalCompanyId.Contains(normalized)) ||
                    (x.Mobile != null && x.Mobile.Contains(normalized)));
            }

            if (!string.IsNullOrWhiteSpace(firstName))
            {
                var normalized = firstName.Trim();
                query = query.Where(x => (x.FirstName != null && x.FirstName.Contains(normalized)) ||
                                         (x.CompanyName != null && x.CompanyName.Contains(normalized)));
            }

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                var normalized = lastName.Trim();
                query = query.Where(x => x.LastName != null && x.LastName.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                var normalized = nationalId.Trim();
                query = query.Where(x => (x.NationalId != null && x.NationalId.Contains(normalized)) ||
                                         (x.NationalCompanyId != null && x.NationalCompanyId.Contains(normalized)));
            }

            if (!string.IsNullOrWhiteSpace(columnFilterField) && !string.IsNullOrWhiteSpace(columnFilterValue))
            {
                var value = columnFilterValue.Trim();
                query = columnFilterField switch
                {
                    "fullName" => QueryableColumnFilter.ApplyString(query, x =>
                        x.TaxpayerType == TaxpayerType.LegalEntity
                            ? x.CompanyName
                            : (x.FirstName ?? "") + " " + (x.LastName ?? ""), columnFilterOperator, value),
                    "identifier" => QueryableColumnFilter.ApplyString(query, x => x.NationalId ?? x.NationalCompanyId, columnFilterOperator, value),
                    "mobile" => QueryableColumnFilter.ApplyString(query, x => x.Mobile, columnFilterOperator, value),
                    "taxpayerType" => ApplyTaxpayerTypeFilter(query, value, columnFilterOperator),
                    _ => query
                };
            }

            query = query.OrderBy(x => x.TaxpayerType)
                .ThenBy(x => x.LastName ?? x.CompanyName)
                .ThenBy(x => x.FirstName)
                .ThenBy(x => x.NationalId ?? x.NationalCompanyId);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TaxpayerListItemDto(
                    x.Id,
                    x.TaxpayerType,
                    x.FirstName,
                    x.LastName,
                    x.NationalId,
                    x.CompanyName,
                    x.NationalCompanyId,
                    x.Mobile))
                .ToListAsync(cancellationToken);

            return new PagedResponse<TaxpayerListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public Task<TaxpayerDetailsDto?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.Taxpayers
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new TaxpayerDetailsDto(
                    x.Id,
                    x.TaxpayerType,
                    x.FirstName,
                    x.LastName,
                    x.NationalId,
                    x.CompanyName,
                    x.NationalCompanyId,
                    x.IdentityNumber,
                    x.Mobile,
                    x.LandlinePhone,
                    x.PostalCode,
                    x.Address,
                    x.CreatedAt,
                    x.ModifiedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static IQueryable<Taxpayer> ApplyTaxpayerTypeFilter(IQueryable<Taxpayer> query, string value, string? filterOperator)
        {
            var type = value.Contains("حقوقی", StringComparison.OrdinalIgnoreCase) || value.Contains("Legal", StringComparison.OrdinalIgnoreCase)
                ? TaxpayerType.LegalEntity
                : value.Contains("حقیقی", StringComparison.OrdinalIgnoreCase) || value.Contains("Natural", StringComparison.OrdinalIgnoreCase)
                    ? TaxpayerType.NaturalPerson
                    : (TaxpayerType?)null;
            if (type is null) return query.Where(_ => false);
            return filterOperator is "doesNotEqual" or "doesNotContain"
                ? query.Where(x => x.TaxpayerType != type.Value)
                : query.Where(x => x.TaxpayerType == type.Value);
        }

        public Task<Taxpayer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.Taxpayers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<bool> ExistsByNationalIdAsync(string nationalId, Guid? excludingId = null, CancellationToken cancellationToken = default)
        {
            var normalized = nationalId.Trim();
            return _dbContext.Taxpayers
                .IgnoreQueryFilters()
                .AnyAsync(x => x.NationalId == normalized && x.DeletedAt == null && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
        }

        public Task<bool> ExistsByNationalCompanyIdAsync(string nationalCompanyId, Guid? excludingId = null, CancellationToken cancellationToken = default)
        {
            var normalized = nationalCompanyId.Trim();
            return _dbContext.Taxpayers
                .IgnoreQueryFilters()
                .AnyAsync(x => x.NationalCompanyId == normalized && x.DeletedAt == null && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
        }

        public Task<bool> ExistsByMobileAsync(string mobile, Guid? excludingId = null, CancellationToken cancellationToken = default)
        {
            var normalized = mobile.Trim();
            return _dbContext.Taxpayers
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Mobile == normalized && x.DeletedAt == null && (!excludingId.HasValue || x.Id != excludingId.Value), cancellationToken);
        }

        public async Task AddAsync(Taxpayer taxpayer, CancellationToken cancellationToken = default)
        {
            await _dbContext.Taxpayers.AddAsync(taxpayer, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
