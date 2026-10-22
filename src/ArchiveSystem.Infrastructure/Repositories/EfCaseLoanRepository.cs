using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.CaseLoans.Interfaces;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public sealed class EfCaseLoanRepository : ICaseLoanRepository
    {
        private readonly ArchiveDbContext _dbContext;

        public EfCaseLoanRepository(ArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PagedResponse<CaseLoanListItemDto>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? caseNumber,
            string? nationalId,
            string? borrowerLastName,
            string? columnFilterField,
            string? columnFilterOperator,
            string? columnFilterValue,
            CancellationToken cancellationToken = default)
        {
            var query = _dbContext.CaseLoans.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(caseNumber))
            {
                var normalized = caseNumber.Trim();
                query = query.Where(loan =>
                    loan.CaseNumber.Contains(normalized) ||
                    _dbContext.Cases.Any(caseEntity =>
                        caseEntity.DeletedAt == null &&
                        caseEntity.CaseNumber == loan.CaseNumber &&
                        caseEntity.ExternalCaseNumber != null &&
                        caseEntity.ExternalCaseNumber.Contains(normalized)));
            }

            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                var normalized = nationalId.Trim();
                query = query.Where(x => x.NationalId.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(borrowerLastName))
            {
                var normalized = borrowerLastName.Trim();
                query = query.Where(x => x.BorrowerLastName.Contains(normalized));
            }

            if (!string.IsNullOrWhiteSpace(columnFilterField) && !string.IsNullOrWhiteSpace(columnFilterValue))
            {
                var value = columnFilterValue.Trim();
                query = columnFilterField switch
                {
                    "caseNumber" => QueryableColumnFilter.ApplyString(query, x => x.CaseNumber, columnFilterOperator, value),
                    "borrowerFullName" => QueryableColumnFilter.ApplyString(query, x => x.BorrowerFirstName + " " + x.BorrowerLastName, columnFilterOperator, value),
                    "receiverUnit" => QueryableColumnFilter.ApplyString(query, x => x.ReceiverUnit, columnFilterOperator, value),
                    "registeredBy" => QueryableColumnFilter.ApplyString(query, x => x.RegisteredBy, columnFilterOperator, value),
                    "nationalId" => QueryableColumnFilter.ApplyString(query, x => x.NationalId, columnFilterOperator, value),
                    "contactNumber" => QueryableColumnFilter.ApplyString(query, x => x.ContactNumber, columnFilterOperator, value),
                    "status" => ApplyStatusFilter(query, value, columnFilterOperator),
                    _ => query
                };
            }

            query = query.OrderByDescending(x => x.CheckoutDate).ThenByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CaseLoanListItemDto(
                    x.Id,
                    x.BorrowerFirstName,
                    x.BorrowerLastName,
                    x.BorrowerFirstName + " " + x.BorrowerLastName,
                    x.CaseNumber,
                    x.NationalId,
                    x.ContactNumber,
                    x.CheckoutDate,
                    x.ExpectedReturnDate,
                    x.ActualReturnDate,
                    x.Status,
                    x.ReceiverUnit,
                    x.RegisteredBy,
                    x.IsReturned,
                    x.Description,
                    x.CreatedAt,
                    x.ModifiedAt))
                .ToListAsync(cancellationToken);

            return new PagedResponse<CaseLoanListItemDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public Task<CaseLoanDetailsDto?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.CaseLoans
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new CaseLoanDetailsDto(
                    x.Id,
                    x.BorrowerFirstName,
                    x.BorrowerLastName,
                    x.CaseNumber,
                    x.NationalId,
                    x.ContactNumber,
                    x.CheckoutDate,
                    x.ExpectedReturnDate,
                    x.ActualReturnDate,
                    x.Status,
                    x.ReceiverUnit,
                    x.RegisteredBy,
                    x.Description,
                    x.IsReturned,
                    x.CreatedAt,
                    x.ModifiedAt))
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static IQueryable<CaseLoan> ApplyStatusFilter(IQueryable<CaseLoan> query, string value, string? filterOperator)
        {
            var status = value.Contains("برگشت", StringComparison.OrdinalIgnoreCase) ? "Returned"
                : value.Contains("دیرکرد", StringComparison.OrdinalIgnoreCase) ? "Overdue"
                : value.Contains("امانت", StringComparison.OrdinalIgnoreCase) || value.Contains("Loaned", StringComparison.OrdinalIgnoreCase) ? "Loaned"
                : value.Contains("Returned", StringComparison.OrdinalIgnoreCase) ? "Returned"
                : value.Contains("Overdue", StringComparison.OrdinalIgnoreCase) ? "Overdue"
                : null;
            if (status is null) return query.Where(_ => false);
            return filterOperator is "doesNotEqual" or "doesNotContain"
                ? query.Where(x => x.Status != status)
                : query.Where(x => x.Status == status);
        }

        public Task<CaseLoan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.CaseLoans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<CaseLoan?> GetLatestByCaseNumberAsync(
            string caseNumber,
            CancellationToken cancellationToken = default)
        {
            var normalized = caseNumber.Trim();
            return _dbContext.CaseLoans
                .AsNoTracking()
                .Where(x => x.CaseNumber == normalized)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.CheckoutDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(CaseLoan caseLoan, CancellationToken cancellationToken = default)
        {
            await _dbContext.CaseLoans.AddAsync(caseLoan, cancellationToken);
        }

        public void Remove(CaseLoan caseLoan)
        {
            _dbContext.CaseLoans.Remove(caseLoan);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
