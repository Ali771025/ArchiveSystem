using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Owners.Interfaces;
using ArchiveSystem.Application.Features.Owners.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories;

public sealed class EfOwnerRepository : IOwnerRepository
{
    private const string PersianSearchCollation = "Persian_100_CI_AS";
    private const string OwnersUnavailableMessage =
        "Owner management is unavailable because the Owners table is missing. Apply latest database migrations.";

    private readonly ArchiveDbContext _dbContext;
    private bool? _hasOwnersTable;

    public EfOwnerRepository(ArchiveDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OwnerDto> CreateAsync(CreateOwnerDto request, CancellationToken cancellationToken = default)
    {
        await EnsureOwnersTableAsync(cancellationToken);

        try
        {
            var naturalCode = NormalizeOptional(request.NationalCode);
            var legalNationalId = NormalizeOptional(request.NationalId);
            await EnsureNoDuplicateIdentifierAsync(null, naturalCode, legalNationalId, cancellationToken);

            var owner = new Owner(
                id: 0,
                ownerType: request.OwnerType,
                mobile: NormalizeOptional(request.Mobile),
                postalCode: NormalizeOptional(request.PostalCode),
                address: NormalizeOptional(request.Address),
                homePhone: NormalizeOptional(request.HomePhone),
                firstName: NormalizeOptional(request.FirstName),
                lastName: NormalizeOptional(request.LastName),
                nationalCode: naturalCode,
                identityNumber: NormalizeOptional(request.IdentityNumber),
                companyName: NormalizeOptional(request.CompanyName),
                nationalId: legalNationalId,
                registrationNumber: NormalizeOptional(request.RegistrationNumber),
                taxpayerId: NormalizeOptional(request.TaxpayerId));

            _dbContext.Owners.Add(owner);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapOwner(owner);
        }
        catch (Exception exception) when (IsOwnersTableMissing(exception))
        {
            _hasOwnersTable = false;
            throw new ServiceUnavailableException(OwnersUnavailableMessage, exception);
        }
    }

    public async Task<OwnerDto?> GetByIdAsync(long ownerId, CancellationToken cancellationToken = default)
    {
        if (!await HasOwnersTableAsync(cancellationToken))
        {
            throw new ServiceUnavailableException(OwnersUnavailableMessage);
        }

        try
        {
            var owner = await _dbContext.Owners
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ownerId, cancellationToken);

            return owner == null ? null : MapOwner(owner);
        }
        catch (Exception exception) when (IsOwnersTableMissing(exception))
        {
            _hasOwnersTable = false;
            throw new ServiceUnavailableException(OwnersUnavailableMessage, exception);
        }
    }

    public async Task<OwnerDto> UpdateAsync(long ownerId, CreateOwnerDto request, CancellationToken cancellationToken = default)
    {
        await EnsureOwnersTableAsync(cancellationToken);

        try
        {
            var owner = await _dbContext.Owners
                .FirstOrDefaultAsync(x => x.Id == ownerId, cancellationToken);

            if (owner == null)
            {
                throw new NotFoundException($"Owner '{ownerId}' was not found.");
            }

            var naturalCode = NormalizeOptional(request.NationalCode);
            var legalNationalId = NormalizeOptional(request.NationalId);
            await EnsureNoDuplicateIdentifierAsync(ownerId, naturalCode, legalNationalId, cancellationToken);

            owner.Update(
                ownerType: request.OwnerType,
                mobile: NormalizeOptional(request.Mobile),
                postalCode: NormalizeOptional(request.PostalCode),
                address: NormalizeOptional(request.Address),
                homePhone: NormalizeOptional(request.HomePhone),
                firstName: NormalizeOptional(request.FirstName),
                lastName: NormalizeOptional(request.LastName),
                nationalCode: naturalCode,
                identityNumber: NormalizeOptional(request.IdentityNumber),
                companyName: NormalizeOptional(request.CompanyName),
                nationalId: legalNationalId,
                registrationNumber: NormalizeOptional(request.RegistrationNumber),
                taxpayerId: NormalizeOptional(request.TaxpayerId));

            await _dbContext.SaveChangesAsync(cancellationToken);
            return MapOwner(owner);
        }
        catch (ArgumentException exception)
        {
            throw new ValidationException(exception.Message);
        }
        catch (Exception exception) when (IsOwnersTableMissing(exception))
        {
            _hasOwnersTable = false;
            throw new ServiceUnavailableException(OwnersUnavailableMessage, exception);
        }
    }

    public async Task<PaginatedResult<OwnerDto>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = page < 1 ? 1 : page;
        var safePageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 200);

        if (!await HasOwnersTableAsync(cancellationToken))
        {
            return new PaginatedResult<OwnerDto>
            {
                Items = new List<OwnerDto>(),
                TotalCount = 0,
                Page = safePage,
                PageSize = safePageSize
            };
        }

        try
        {
            var query = _dbContext.Owners.AsNoTracking();
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(owner => owner.CompanyName)
                .ThenBy(owner => owner.FirstName)
                .ThenBy(owner => owner.LastName)
                .ThenBy(owner => owner.Id)
                .Skip((safePage - 1) * safePageSize)
                .Take(safePageSize)
                .Select(owner => new OwnerDto
                {
                    Id = owner.Id,
                    OwnerType = owner.OwnerType,
                    Mobile = owner.Mobile,
                    PostalCode = owner.PostalCode,
                    Address = owner.Address,
                    HomePhone = owner.HomePhone,
                    FirstName = owner.FirstName,
                    LastName = owner.LastName,
                    NationalCode = owner.NationalCode,
                    IdentityNumber = owner.IdentityNumber,
                    CompanyName = owner.CompanyName,
                    NationalId = owner.NationalId,
                    RegistrationNumber = owner.RegistrationNumber,
                    TaxpayerId = owner.TaxpayerId
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResult<OwnerDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = safePage,
                PageSize = safePageSize
            };
        }
        catch (Exception exception) when (IsOwnersTableMissing(exception))
        {
            _hasOwnersTable = false;
            return new PaginatedResult<OwnerDto>
            {
                Items = new List<OwnerDto>(),
                TotalCount = 0,
                Page = safePage,
                PageSize = safePageSize
            };
        }
    }

    public async Task<IReadOnlyCollection<OwnerSearchItemDto>> SearchAsync(
        string? query,
        int take,
        CancellationToken cancellationToken = default)
    {
        if (!await HasOwnersTableAsync(cancellationToken) || string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<OwnerSearchItemDto>();
        }

        try
        {
            var normalizedQuery = NormalizePersianSearchText(query);
            var compactQuery = normalizedQuery.Replace(" ", string.Empty, StringComparison.Ordinal);
            var escaped = EscapeLikePattern(normalizedQuery);
            var escapedCompact = EscapeLikePattern(compactQuery);
            var containsPattern = $"%{escaped}%";
            var containsCompactPattern = $"%{escapedCompact}%";
            var startsWithPattern = $"{escaped}%";
            var safeTake = take < 1 ? 20 : Math.Min(take, 50);

            var rows = await _dbContext.Owners
                .AsNoTracking()
                .Where(owner =>
                    EF.Functions.Like(EF.Functions.Collate((owner.FirstName ?? string.Empty).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), containsPattern) ||
                    EF.Functions.Like(EF.Functions.Collate((owner.LastName ?? string.Empty).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), containsPattern) ||
                    EF.Functions.Like(EF.Functions.Collate(((owner.FirstName ?? string.Empty) + " " + (owner.LastName ?? string.Empty)).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), containsPattern) ||
                    EF.Functions.Like(EF.Functions.Collate(((owner.LastName ?? string.Empty) + " " + (owner.FirstName ?? string.Empty)).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), containsPattern) ||
                    EF.Functions.Like(EF.Functions.Collate((owner.CompanyName ?? string.Empty).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), containsPattern) ||
                    EF.Functions.Like(EF.Functions.Collate(((owner.FirstName ?? string.Empty) + (owner.LastName ?? string.Empty)).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " ").Replace(" ", string.Empty), PersianSearchCollation), containsCompactPattern) ||
                    EF.Functions.Like(EF.Functions.Collate(((owner.LastName ?? string.Empty) + (owner.FirstName ?? string.Empty)).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " ").Replace(" ", string.Empty), PersianSearchCollation), containsCompactPattern) ||
                    EF.Functions.Like(EF.Functions.Collate((owner.CompanyName ?? string.Empty).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " ").Replace(" ", string.Empty), PersianSearchCollation), containsCompactPattern) ||
                    EF.Functions.Like(owner.NationalCode ?? string.Empty, startsWithPattern) ||
                    EF.Functions.Like(owner.NationalId ?? string.Empty, startsWithPattern) ||
                    EF.Functions.Like(owner.TaxpayerId ?? string.Empty, startsWithPattern))
                .OrderBy(owner =>
                    (owner.NationalCode ?? string.Empty) == normalizedQuery ||
                    (owner.NationalId ?? string.Empty) == normalizedQuery ||
                    (owner.TaxpayerId ?? string.Empty) == normalizedQuery ? 0 :
                    EF.Functions.Like(owner.NationalCode ?? string.Empty, startsWithPattern) ||
                    EF.Functions.Like(owner.NationalId ?? string.Empty, startsWithPattern) ||
                    EF.Functions.Like(owner.TaxpayerId ?? string.Empty, startsWithPattern) ? 1 :
                    EF.Functions.Like(EF.Functions.Collate((owner.CompanyName ?? string.Empty).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), startsWithPattern) ||
                    EF.Functions.Like(EF.Functions.Collate((owner.FirstName ?? string.Empty).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), startsWithPattern) ||
                    EF.Functions.Like(EF.Functions.Collate(((owner.FirstName ?? string.Empty) + " " + (owner.LastName ?? string.Empty)).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), startsWithPattern) ||
                    EF.Functions.Like(EF.Functions.Collate((owner.LastName ?? string.Empty).Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Replace("\u200c", " "), PersianSearchCollation), startsWithPattern) ? 2 : 3)
                .ThenBy(owner => owner.CompanyName)
                .ThenBy(owner => owner.FirstName)
                .ThenBy(owner => owner.LastName)
                .ThenBy(owner => owner.Id)
                .Take(safeTake)
                .Select(owner => new
                {
                    owner.Id,
                    owner.OwnerType,
                    owner.FirstName,
                    owner.LastName,
                    owner.CompanyName,
                    owner.NationalCode,
                    owner.NationalId,
                    owner.TaxpayerId,
                    owner.Mobile
                })
                .ToListAsync(cancellationToken);

            return rows.Select(owner =>
            {
                var nameOrTitle = owner.OwnerType == OwnerType.Natural
                    ? $"{owner.FirstName} {owner.LastName}".Trim()
                    : owner.CompanyName?.Trim() ?? string.Empty;
                var nationalIdentifier = owner.OwnerType == OwnerType.Natural
                    ? owner.NationalCode?.Trim() ?? string.Empty
                    : owner.NationalId?.Trim() ?? string.Empty;

                return new OwnerSearchItemDto
                {
                    Id = owner.Id,
                    OwnerType = owner.OwnerType,
                    NameOrTitle = nameOrTitle,
                    NationalIdentifier = nationalIdentifier,
                    TaxpayerId = owner.TaxpayerId,
                    Label = BuildSearchLabel(nameOrTitle, nationalIdentifier, owner.TaxpayerId),
                    Mobile = owner.Mobile
                };
            }).ToList();
        }
        catch (Exception exception) when (IsOwnersTableMissing(exception))
        {
            _hasOwnersTable = false;
            return Array.Empty<OwnerSearchItemDto>();
        }
    }

    private static string NormalizePersianSearchText(string value)
    {
        return value.Trim()
            .Replace('ي', 'ی')
            .Replace('ى', 'ی')
            .Replace('ك', 'ک')
            .Replace('\u200c', ' ');
    }

    private static string BuildSearchLabel(string nameOrTitle, string nationalIdentifier, string? taxpayerId)
    {
        var identifiers = new[] { nationalIdentifier, taxpayerId?.Trim() }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return identifiers.Length == 0
            ? nameOrTitle
            : $"{nameOrTitle} - {string.Join(" / ", identifiers)}";
    }

    private async Task EnsureNoDuplicateIdentifierAsync(
        long? currentOwnerId,
        string? naturalCode,
        string? legalNationalId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(naturalCode))
        {
            var duplicateNatural = await _dbContext.Owners
                .AsNoTracking()
                .AnyAsync(owner => owner.Id != currentOwnerId && owner.NationalCode == naturalCode, cancellationToken);
            if (duplicateNatural)
            {
                throw new ConflictException($"Owner with national code '{naturalCode}' already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(legalNationalId))
        {
            var duplicateLegal = await _dbContext.Owners
                .AsNoTracking()
                .AnyAsync(owner => owner.Id != currentOwnerId && owner.NationalId == legalNationalId, cancellationToken);
            if (duplicateLegal)
            {
                throw new ConflictException($"Owner with national id '{legalNationalId}' already exists.");
            }
        }
    }

    private async Task EnsureOwnersTableAsync(CancellationToken cancellationToken)
    {
        if (!await HasOwnersTableAsync(cancellationToken))
        {
            throw new ServiceUnavailableException(OwnersUnavailableMessage);
        }
    }

    private async Task<bool> HasOwnersTableAsync(CancellationToken cancellationToken)
    {
        if (_hasOwnersTable.HasValue)
        {
            return _hasOwnersTable.Value;
        }

        if (!_dbContext.Database.IsRelational())
        {
            _hasOwnersTable = true;
            return true;
        }

        var result = await _dbContext.Database
            .SqlQueryRaw<int>(
                "SELECT CAST(CASE WHEN OBJECT_ID(N'[dbo].[Owners]', N'U') IS NULL THEN 0 ELSE 1 END AS int) AS [Value]")
            .SingleAsync(cancellationToken);

        _hasOwnersTable = result == 1;
        return _hasOwnersTable.Value;
    }

    private static OwnerDto MapOwner(Owner owner)
    {
        return new OwnerDto
        {
            Id = owner.Id,
            OwnerType = owner.OwnerType,
            Mobile = owner.Mobile,
            PostalCode = owner.PostalCode,
            Address = owner.Address,
            HomePhone = owner.HomePhone,
            FirstName = owner.FirstName,
            LastName = owner.LastName,
            NationalCode = owner.NationalCode,
            IdentityNumber = owner.IdentityNumber,
            CompanyName = owner.CompanyName,
            NationalId = owner.NationalId,
            RegistrationNumber = owner.RegistrationNumber,
            TaxpayerId = owner.TaxpayerId
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string EscapeLikePattern(string input)
    {
        return input
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("_", "[_]", StringComparison.Ordinal);
    }

    private static bool IsOwnersTableMissing(Exception exception)
    {
        if (exception is SqlException sqlException)
        {
            return sqlException.Number == 208 &&
                   sqlException.Message.Contains("Owners", StringComparison.OrdinalIgnoreCase);
        }

        return exception.InnerException is not null && IsOwnersTableMissing(exception.InnerException);
    }
}
