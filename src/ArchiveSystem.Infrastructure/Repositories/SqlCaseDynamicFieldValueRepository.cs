using System.Globalization;
using System.Text.Json;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class SqlCaseDynamicFieldValueRepository : ICaseDynamicFieldValueRepository
    {
        private const string PersianSearchCollation = "Persian_100_CI_AS";
        private readonly ArchiveDbContext _context;

        public SqlCaseDynamicFieldValueRepository(ArchiveDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyDictionary<Guid, CaseDynamicFieldValueDto>> GetCaseValuesByCaseIdAsync(long caseId, CancellationToken cancellationToken = default)
        {
            var result = new Dictionary<Guid, CaseDynamicFieldValueDto>();

            var stringValues = await _context.CaseFieldValueStrings
                .AsNoTracking()
                .Where(v => v.CaseId == caseId)
                .ToListAsync(cancellationToken);
            foreach (var value in stringValues)
            {
                result[value.FieldId] = new CaseDynamicFieldValueDto
                {
                    FieldId = value.FieldId,
                    StringValue = value.Value
                };
            }

            var numberValues = await _context.CaseFieldValueNumbers
                .AsNoTracking()
                .Where(v => v.CaseId == caseId)
                .ToListAsync(cancellationToken);
            foreach (var value in numberValues)
            {
                result[value.FieldId] = new CaseDynamicFieldValueDto
                {
                    FieldId = value.FieldId,
                    NumberValue = value.Value
                };
            }

            var dateValues = await _context.CaseFieldValueDates
                .AsNoTracking()
                .Where(v => v.CaseId == caseId)
                .ToListAsync(cancellationToken);
            foreach (var value in dateValues)
            {
                result[value.FieldId] = new CaseDynamicFieldValueDto
                {
                    FieldId = value.FieldId,
                    DateValue = value.Value
                };
            }

            var boolValues = await _context.CaseFieldValueBools
                .AsNoTracking()
                .Where(v => v.CaseId == caseId)
                .ToListAsync(cancellationToken);
            foreach (var value in boolValues)
            {
                result[value.FieldId] = new CaseDynamicFieldValueDto
                {
                    FieldId = value.FieldId,
                    BoolValue = value.Value
                };
            }

            var optionValues = await _context.CaseFieldValueOptions
                .AsNoTracking()
                .Where(v => v.CaseId == caseId)
                .GroupBy(v => v.FieldId)
                .Select(group => new
                {
                    FieldId = group.Key,
                    OptionIds = group.Select(item => item.OptionId).ToList()
                })
                .ToListAsync(cancellationToken);

            foreach (var optionValue in optionValues)
            {
                result[optionValue.FieldId] = new CaseDynamicFieldValueDto
                {
                    FieldId = optionValue.FieldId,
                    OptionIds = optionValue.OptionIds
                };
            }

            return result;
        }

        public async Task ReplaceCaseValuesAsync(long caseId, IReadOnlyCollection<CaseDynamicFieldValueDto> values, CancellationToken cancellationToken = default)
        {
            _context.CaseFieldValueStrings.RemoveRange(_context.CaseFieldValueStrings.Where(v => v.CaseId == caseId));
            _context.CaseFieldValueNumbers.RemoveRange(_context.CaseFieldValueNumbers.Where(v => v.CaseId == caseId));
            _context.CaseFieldValueDates.RemoveRange(_context.CaseFieldValueDates.Where(v => v.CaseId == caseId));
            _context.CaseFieldValueBools.RemoveRange(_context.CaseFieldValueBools.Where(v => v.CaseId == caseId));
            _context.CaseFieldValueOptions.RemoveRange(_context.CaseFieldValueOptions.Where(v => v.CaseId == caseId));

            foreach (var value in values)
            {
                if (value.OptionIds.Count > 0)
                {
                    foreach (var optionId in value.OptionIds.Distinct())
                    {
                        _context.CaseFieldValueOptions.Add(new CaseFieldValueOption(caseId, value.FieldId, optionId));
                    }

                    continue;
                }

                if (value.StringValue != null)
                {
                    _context.CaseFieldValueStrings.Add(new CaseFieldValueString(caseId, value.FieldId, value.StringValue));
                    continue;
                }

                if (value.NumberValue.HasValue)
                {
                    _context.CaseFieldValueNumbers.Add(new CaseFieldValueNumber(caseId, value.FieldId, value.NumberValue.Value));
                    continue;
                }

                if (value.DateValue.HasValue)
                {
                    _context.CaseFieldValueDates.Add(new CaseFieldValueDate(caseId, value.FieldId, value.DateValue.Value));
                    continue;
                }

                if (value.BoolValue.HasValue)
                {
                    _context.CaseFieldValueBools.Add(new CaseFieldValueBool(caseId, value.FieldId, value.BoolValue.Value));
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<SearchCasesResultDto> SearchCasesAsync(
            IReadOnlyCollection<long> classificationIds,
            bool bypassAuthorizationFilters,
            IReadOnlyCollection<long> allowedClassificationIds,
            long? excludedClassificationId,
            string dynamicFilterLogic,
            IReadOnlyCollection<DynamicFilterGroupDto> dynamicFilterGroups,
            IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto> fieldDefinitions,
            string? ownerName,
            string? externalCaseNumber,
            string? createdBy,
            DateTime? fromDate,
            DateTime? toDate,
            IReadOnlyCollection<long> tags,
            int page,
            int pageSize,
            string? sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Case> query = _context.Cases
                .AsNoTracking()
                .Where(c => c.DeletedAt == null);

            var allowedSet = allowedClassificationIds.Distinct().ToArray();
            if (!bypassAuthorizationFilters && allowedSet.Length == 0)
            {
                return new SearchCasesResultDto
                {
                    Items = new List<CaseSearchItemDto>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize
                };
            }

            if (!bypassAuthorizationFilters)
            {
                query = query.Where(c => allowedSet.Contains(c.ClassificationId));
            }

            var classificationScope = classificationIds.Distinct().ToArray();
            if (classificationScope.Length > 0)
            {
                query = query.Where(c => classificationScope.Contains(c.ClassificationId));
            }
            else if (excludedClassificationId.HasValue)
            {
                query = query.Where(c => c.ClassificationId != excludedClassificationId.Value);
            }

            query = ApplyCaseListFilters(query, externalCaseNumber, createdBy, fromDate, toDate, tags);

            query = ApplyDynamicFilterGroups(
                query,
                dynamicFilterLogic,
                dynamicFilterGroups,
                fieldDefinitions);

            query = await ApplyOwnerNameFilterAsync(query, ownerName, cancellationToken);

            var totalCount = await query.CountAsync(cancellationToken);
            var skip = (page - 1) * pageSize;

            query = ApplySort(query, sortBy, sortDescending);

            var rawItems = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(c => new
                {
                    Id = c.Id,
                    CaseNumber = c.CaseNumber,
                    ExternalCaseNumber = c.ExternalCaseNumber,
                    Title = c.Title,
                    AvatarImagePath = c.AvatarImagePath,
                    Address = c.Location,
                    Owners = c.Owners,
                    ClassificationId = c.ClassificationId,
                    CreatedByUserId = c.CreatedByUserId,
                    CreatedByUserName = c.CreatedByUserName,
                    FileCount = c.Files.Count(file => file.DeletedAt == null),
                    CreatedAt = c.CreatedAt,
                    ModifiedAt = c.ModifiedAt,
                    ClosedAt = c.ClosedAt
                })
                .ToListAsync(cancellationToken);

            var caseNumbers = rawItems
                .Select(c => c.CaseNumber)
                .Where(caseNumber => !string.IsNullOrWhiteSpace(caseNumber))
                .Distinct()
                .ToArray();

            var latestLoanStatusByCaseNumber = await _context.CaseLoans
                .AsNoTracking()
                .Where(loan => caseNumbers.Contains(loan.CaseNumber))
                .GroupBy(loan => loan.CaseNumber)
                .Select(group => new
                {
                    CaseNumber = group.Key,
                    Status = group
                        .OrderByDescending(loan => loan.CheckoutDate)
                        .ThenByDescending(loan => loan.CreatedAt)
                        .Select(loan => loan.Status)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(item => item.CaseNumber, item => item.Status, cancellationToken);

            var items = rawItems.Select(c => new CaseSearchItemDto
            {
                Id = c.Id,
                CaseNumber = c.CaseNumber,
                Title = c.Title,
                AvatarImagePath = c.AvatarImagePath,
                Address = c.Address,
                ExternalCaseNumber = c.ExternalCaseNumber,
                OwnerNames = ParseOwnerNames(c.Owners),
                ClassificationId = c.ClassificationId,
                CreatedByUserId = c.CreatedByUserId,
                CreatedByUserName = c.CreatedByUserName,
                LoanStatus = latestLoanStatusByCaseNumber.GetValueOrDefault(c.CaseNumber),
                FileCount = c.FileCount,
                CreatedAt = c.CreatedAt,
                ModifiedAt = c.ModifiedAt,
                ClosedAt = c.ClosedAt
            }).ToList();

            return new SearchCasesResultDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static IQueryable<Case> ApplySort(IQueryable<Case> query, string? sortBy, bool sortDescending)
        {
            return (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "casenumber" => sortDescending ? query.OrderByDescending(c => c.CaseNumber) : query.OrderBy(c => c.CaseNumber),
                "externalcasenumber" => sortDescending ? query.OrderByDescending(c => c.ExternalCaseNumber) : query.OrderBy(c => c.ExternalCaseNumber),
                "title" => sortDescending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
                "address" => sortDescending ? query.OrderByDescending(c => c.Location) : query.OrderBy(c => c.Location),
                "classificationtitle" => sortDescending
                    ? query.OrderByDescending(c => c.Classification.Path).ThenByDescending(c => c.Classification.Title)
                    : query.OrderBy(c => c.Classification.Path).ThenBy(c => c.Classification.Title),
                "createdat" => sortDescending ? query.OrderByDescending(c => c.CreatedAt) : query.OrderBy(c => c.CreatedAt),
                "updatedat" => sortDescending ? query.OrderByDescending(c => c.ModifiedAt ?? c.CreatedAt) : query.OrderBy(c => c.ModifiedAt ?? c.CreatedAt),
                _ => sortDescending ? query.OrderByDescending(c => c.ModifiedAt ?? c.CreatedAt) : query.OrderBy(c => c.ModifiedAt ?? c.CreatedAt)
            };
        }

        private IQueryable<Case> ApplyCaseListFilters(
            IQueryable<Case> query,
            string? externalCaseNumber,
            string? createdBy,
            DateTime? fromDate,
            DateTime? toDate,
            IReadOnlyCollection<long> tags)
        {
            var normalizedCaseNumber = externalCaseNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedCaseNumber))
            {
                query = query.Where(c =>
                    c.ExternalCaseNumber.Contains(normalizedCaseNumber) ||
                    c.CaseNumber.Contains(normalizedCaseNumber));
            }

            var normalizedCreatedBy = createdBy?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedCreatedBy))
            {
                if (long.TryParse(normalizedCreatedBy, out var createdByUserId))
                {
                    query = query.Where(c => c.CreatedByUserId == createdByUserId);
                }
                else
                {
                    query = query.Where(c =>
                        c.CreatedByUserName != null &&
                        EF.Functions.Like(c.CreatedByUserName, $"%{normalizedCreatedBy}%"));
                }
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(c => (c.ModifiedAt ?? c.CreatedAt) >= from);
            }

            if (toDate.HasValue)
            {
                var exclusiveTo = toDate.Value.Date.AddDays(1);
                query = query.Where(c => (c.ModifiedAt ?? c.CreatedAt) < exclusiveTo);
            }

            var tagIds = tags?
                .Where(tagId => tagId > 0)
                .Distinct()
                .ToArray() ?? Array.Empty<long>();

            if (tagIds.Length > 0)
            {
                query = query.Where(c => c.Files.Any(file =>
                    file.DeletedAt == null &&
                    file.FileTags.Any(fileTag => tagIds.Contains(fileTag.TagId))));
            }

            return query;
        }

        private async Task<IQueryable<Case>> ApplyOwnerNameFilterAsync(
            IQueryable<Case> query,
            string? ownerName,
            CancellationToken cancellationToken)
        {
            var normalizedOwnerName = NormalizePersianSearchText(ownerName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalizedOwnerName))
            {
                return query;
            }

            var compactOwnerName = normalizedOwnerName.Replace(" ", string.Empty, StringComparison.Ordinal);
            var candidates = await query
                .Select(c => new { c.Id, c.Owners })
                .ToListAsync(cancellationToken);

            var matchingIds = candidates
                .Where(candidate => ParseOwnerNames(candidate.Owners).Any(name =>
                {
                    var normalizedName = NormalizePersianSearchText(name);
                    var compactName = normalizedName.Replace(" ", string.Empty, StringComparison.Ordinal);
                    return normalizedName.Contains(normalizedOwnerName, StringComparison.OrdinalIgnoreCase) ||
                           compactName.Contains(compactOwnerName, StringComparison.OrdinalIgnoreCase);
                }))
                .Select(candidate => candidate.Id)
                .ToArray();

            return matchingIds.Length == 0
                ? query.Where(c => false)
                : query.Where(c => matchingIds.Contains(c.Id));
        }

        private static string NormalizePersianSearchText(string value)
        {
            return value.Trim()
                .Replace('ي', 'ی')
                .Replace('ى', 'ی')
                .Replace('ك', 'ک')
                .Replace('\u200c', ' ');
        }

        private static List<string> ParseOwnerNames(string? ownersJson) =>
            ArchiveSystem.Application.Common.OwnerDisplayNameParser.ParseNames(ownersJson).ToList();

        private IQueryable<Case> ApplyDynamicFilterGroups(
            IQueryable<Case> baseQuery,
            string logic,
            IReadOnlyCollection<DynamicFilterGroupDto> groups,
            IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto> fieldDefinitions)
        {
            var nonEmptyGroups = groups.Where(group => group.Filters.Count > 0).ToList();
            if (nonEmptyGroups.Count == 0)
            {
                return baseQuery;
            }

            if (!string.Equals(logic, "or", StringComparison.OrdinalIgnoreCase))
            {
                return nonEmptyGroups.Aggregate(
                    baseQuery,
                    (current, group) => ApplyDynamicFilterGroup(current, group, fieldDefinitions));
            }

            IQueryable<long>? matchingIds = null;
            foreach (var group in nonEmptyGroups)
            {
                var groupIds = ApplyDynamicFilterGroup(
                        _context.Cases.AsNoTracking().Where(caseEntity => caseEntity.DeletedAt == null),
                        group,
                        fieldDefinitions)
                    .Select(caseEntity => caseEntity.Id);
                matchingIds = matchingIds == null ? groupIds : matchingIds.Union(groupIds);
            }

            return matchingIds == null
                ? baseQuery
                : baseQuery.Where(caseEntity => matchingIds.Contains(caseEntity.Id));
        }

        private IQueryable<Case> ApplyDynamicFilterGroup(
            IQueryable<Case> baseQuery,
            DynamicFilterGroupDto group,
            IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto> fieldDefinitions)
        {
            var filters = group.Filters
                .Where(filter => fieldDefinitions.ContainsKey(filter.FieldId))
                .ToList();
            if (filters.Count == 0)
            {
                return baseQuery;
            }

            if (!string.Equals(group.Logic, "or", StringComparison.OrdinalIgnoreCase))
            {
                return filters.Aggregate(
                    baseQuery,
                    (current, filter) => ApplyDynamicFilter(
                        current,
                        filter,
                        fieldDefinitions[filter.FieldId]));
            }

            IQueryable<long>? matchingIds = null;
            foreach (var filter in filters)
            {
                var filterIds = ApplyDynamicFilter(
                        _context.Cases.AsNoTracking().Where(caseEntity => caseEntity.DeletedAt == null),
                        filter,
                        fieldDefinitions[filter.FieldId])
                    .Select(caseEntity => caseEntity.Id);
                matchingIds = matchingIds == null ? filterIds : matchingIds.Union(filterIds);
            }

            return matchingIds == null
                ? baseQuery
                : baseQuery.Where(caseEntity => matchingIds.Contains(caseEntity.Id));
        }

        private IQueryable<Case> ApplyDynamicFilter(
            IQueryable<Case> query,
            DynamicFilterDto filter,
            DynamicFieldDefinitionDto field)
        {
            var op = NormalizeOperator(filter.Op);
            return field.FieldType switch
            {
                DynamicFieldType.String or DynamicFieldType.MultiLineText =>
                    ApplyStringFilter(query, filter.FieldId, op, filter),
                DynamicFieldType.Number or DynamicFieldType.Decimal =>
                    ApplyNumberFilter(query, filter.FieldId, op, filter),
                DynamicFieldType.Date =>
                    ApplyDateFilter(query, filter.FieldId, op, filter),
                DynamicFieldType.Bool or DynamicFieldType.CheckBox =>
                    ApplyBoolFilter(query, filter.FieldId, op, filter),
                DynamicFieldType.Enum or DynamicFieldType.MultiSelect =>
                    ApplyEnumFilter(query, filter.FieldId, op, filter),
                _ => query
            };
        }

        private IQueryable<Case> ApplyStringFilter(IQueryable<Case> query, Guid fieldId, string op, DynamicFilterDto filter)
        {
            switch (op)
            {
                case "equals":
                {
                    var value = ReadString(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueStrings.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "notequals":
                {
                    var value = ReadString(filter.Value);
                    return query.Where(c =>
                        !_context.CaseFieldValueStrings.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "contains":
                {
                    var value = ReadString(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueStrings.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && EF.Functions.Like(v.Value, $"%{EscapeLikePattern(value)}%")));
                }
                case "in":
                {
                    var values = ReadStringList(filter).Distinct().ToList();
                    return query.Where(c =>
                        _context.CaseFieldValueStrings.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && values.Contains(v.Value)));
                }
                case "isempty":
                    return query.Where(c =>
                        !_context.CaseFieldValueStrings.Any(v => v.CaseId == c.Id && v.FieldId == fieldId));
                default:
                    return query;
            }
        }

        private IQueryable<Case> ApplyNumberFilter(IQueryable<Case> query, Guid fieldId, string op, DynamicFilterDto filter)
        {
            switch (op)
            {
                case "equals":
                {
                    var value = ReadDecimal(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "notequals":
                {
                    var value = ReadDecimal(filter.Value);
                    return query.Where(c =>
                        !_context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "greaterthan":
                {
                    var value = ReadDecimal(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value > value));
                }
                case "greaterthanorequal":
                {
                    var value = ReadDecimal(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value >= value));
                }
                case "lessthan":
                {
                    var value = ReadDecimal(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value < value));
                }
                case "lessthanorequal":
                {
                    var value = ReadDecimal(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value <= value));
                }
                case "in":
                {
                    var values = ReadDecimalList(filter).Distinct().ToList();
                    return query.Where(c =>
                        _context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && values.Contains(v.Value)));
                }
                case "range":
                {
                    var range = ReadDecimalRange(filter);
                    return query.Where(c =>
                        _context.CaseFieldValueNumbers.Any(v =>
                            v.CaseId == c.Id && v.FieldId == fieldId && v.Value >= range.min && v.Value <= range.max));
                }
                case "isempty":
                    return query.Where(c =>
                        !_context.CaseFieldValueNumbers.Any(v => v.CaseId == c.Id && v.FieldId == fieldId));
                default:
                    return query;
            }
        }

        private IQueryable<Case> ApplyDateFilter(IQueryable<Case> query, Guid fieldId, string op, DynamicFilterDto filter)
        {
            switch (op)
            {
                case "equals":
                {
                    var value = ReadDate(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueDates.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "notequals":
                {
                    var value = ReadDate(filter.Value);
                    return query.Where(c =>
                        !_context.CaseFieldValueDates.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "greaterthan":
                {
                    var value = ReadDate(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueDates.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value > value));
                }
                case "greaterthanorequal":
                {
                    var value = ReadDate(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueDates.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value >= value));
                }
                case "lessthan":
                {
                    var value = ReadDate(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueDates.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value < value));
                }
                case "lessthanorequal":
                {
                    var value = ReadDate(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueDates.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value <= value));
                }
                case "range":
                {
                    var range = ReadDateRange(filter);
                    return query.Where(c =>
                        _context.CaseFieldValueDates.Any(v =>
                            v.CaseId == c.Id && v.FieldId == fieldId && v.Value >= range.from && v.Value <= range.to));
                }
                case "isempty":
                    return query.Where(c =>
                        !_context.CaseFieldValueDates.Any(v => v.CaseId == c.Id && v.FieldId == fieldId));
                default:
                    return query;
            }
        }

        private IQueryable<Case> ApplyBoolFilter(IQueryable<Case> query, Guid fieldId, string op, DynamicFilterDto filter)
        {
            switch (op)
            {
                case "equals":
                {
                    var value = ReadBool(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueBools.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "notequals":
                {
                    var value = ReadBool(filter.Value);
                    return query.Where(c =>
                        !_context.CaseFieldValueBools.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.Value == value));
                }
                case "isempty":
                    return query.Where(c =>
                        !_context.CaseFieldValueBools.Any(v => v.CaseId == c.Id && v.FieldId == fieldId));
                default:
                    return query;
            }
        }

        private IQueryable<Case> ApplyEnumFilter(IQueryable<Case> query, Guid fieldId, string op, DynamicFilterDto filter)
        {
            switch (op)
            {
                case "equals":
                {
                    var value = ReadGuid(filter.Value);
                    return query.Where(c =>
                        _context.CaseFieldValueOptions.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.OptionId == value));
                }
                case "notequals":
                {
                    var value = ReadGuid(filter.Value);
                    return query.Where(c =>
                        !_context.CaseFieldValueOptions.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && v.OptionId == value));
                }
                case "in":
                {
                    var values = ReadGuidList(filter).Distinct().ToList();
                    return query.Where(c =>
                        _context.CaseFieldValueOptions.Any(v => v.CaseId == c.Id && v.FieldId == fieldId && values.Contains(v.OptionId)));
                }
                case "isempty":
                    return query.Where(c =>
                        !_context.CaseFieldValueOptions.Any(v => v.CaseId == c.Id && v.FieldId == fieldId));
                default:
                    return query;
            }
        }

        private static string ReadString(JsonElement? element)
        {
            var current = element ?? throw new ValidationException("Filter value is required.");
            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString() ?? string.Empty,
                JsonValueKind.Number => current.GetRawText(),
                JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
                JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
                _ => throw new ValidationException("Invalid string filter value.")
            };
        }

        private static List<string> ReadStringList(DynamicFilterDto filter)
        {
            if (filter.Values.Count > 0)
            {
                return filter.Values.Select(v => ReadString(v)).ToList();
            }

            if (filter.Value.HasValue && filter.Value.Value.ValueKind == JsonValueKind.Array)
            {
                return filter.Value.Value.EnumerateArray().Select(v => ReadString(v)).ToList();
            }

            return new List<string> { ReadString(filter.Value) };
        }

        private static decimal ReadDecimal(JsonElement? element)
        {
            var current = element ?? throw new ValidationException("Filter value is required.");
            if (current.ValueKind == JsonValueKind.Number && current.TryGetDecimal(out var asNumber))
            {
                return asNumber;
            }

            if (current.ValueKind == JsonValueKind.String &&
                decimal.TryParse(current.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fromString))
            {
                return fromString;
            }

            throw new ValidationException("Invalid numeric filter value.");
        }

        private static List<decimal> ReadDecimalList(DynamicFilterDto filter)
        {
            if (filter.Values.Count > 0)
            {
                return filter.Values.Select(v => ReadDecimal(v)).ToList();
            }

            if (filter.Value.HasValue && filter.Value.Value.ValueKind == JsonValueKind.Array)
            {
                return filter.Value.Value.EnumerateArray().Select(v => ReadDecimal(v)).ToList();
            }

            return new List<decimal> { ReadDecimal(filter.Value) };
        }

        private static (decimal min, decimal max) ReadDecimalRange(DynamicFilterDto filter)
        {
            if (filter.FromValue.HasValue && filter.ToValue.HasValue)
            {
                var from = ReadDecimal(filter.FromValue);
                var to = ReadDecimal(filter.ToValue);
                return from <= to ? (from, to) : (to, from);
            }

            var values = filter.Values.Count > 0
                ? filter.Values
                : filter.Value.HasValue && filter.Value.Value.ValueKind == JsonValueKind.Array
                    ? filter.Value.Value.EnumerateArray().ToList()
                    : new List<JsonElement>();

            if (values.Count != 2)
            {
                throw new ValidationException("'range' requires both FromValue and ToValue, or exactly two values.");
            }

            var left = ReadDecimal(values[0]);
            var right = ReadDecimal(values[1]);
            return left <= right ? (left, right) : (right, left);
        }

        private static DateTime ReadDate(JsonElement? element)
        {
            var current = element ?? throw new ValidationException("Filter value is required.");

            if (current.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(current.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed;
            }

            throw new ValidationException("Invalid date filter value.");
        }

        private static (DateTime from, DateTime to) ReadDateRange(DynamicFilterDto filter)
        {
            if (filter.FromValue.HasValue && filter.ToValue.HasValue)
            {
                var from = ReadDate(filter.FromValue);
                var to = ReadDate(filter.ToValue);
                return from <= to ? (from, to) : (to, from);
            }

            var values = filter.Values.Count > 0
                ? filter.Values
                : filter.Value.HasValue && filter.Value.Value.ValueKind == JsonValueKind.Array
                    ? filter.Value.Value.EnumerateArray().ToList()
                    : new List<JsonElement>();

            if (values.Count != 2)
            {
                throw new ValidationException("'range' requires both FromValue and ToValue, or exactly two values.");
            }

            var left = ReadDate(values[0]);
            var right = ReadDate(values[1]);
            return left <= right ? (left, right) : (right, left);
        }

        private static bool ReadBool(JsonElement? element)
        {
            var current = element ?? throw new ValidationException("Filter value is required.");
            if (current.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return current.GetBoolean();
            }

            if (current.ValueKind == JsonValueKind.String &&
                bool.TryParse(current.GetString(), out var parsed))
            {
                return parsed;
            }

            throw new ValidationException("Invalid boolean filter value.");
        }

        private static Guid ReadGuid(JsonElement? element)
        {
            var current = element ?? throw new ValidationException("Filter value is required.");
            if (current.ValueKind == JsonValueKind.String &&
                Guid.TryParse(current.GetString(), out var parsed))
            {
                return parsed;
            }

            throw new ValidationException("Invalid GUID filter value.");
        }

        private static List<Guid> ReadGuidList(DynamicFilterDto filter)
        {
            if (filter.Values.Count > 0)
            {
                return filter.Values.Select(v => ReadGuid(v)).ToList();
            }

            if (filter.Value.HasValue && filter.Value.Value.ValueKind == JsonValueKind.Array)
            {
                return filter.Value.Value.EnumerateArray().Select(v => ReadGuid(v)).ToList();
            }

            return new List<Guid> { ReadGuid(filter.Value) };
        }

        private static string NormalizeOperator(string? op)
        {
            var normalized = op?.Trim().ToLowerInvariant() ?? string.Empty;
            return normalized switch
            {
                "eq" => "equals",
                "neq" => "notequals",
                "gt" => "greaterthan",
                "gte" => "greaterthanorequal",
                "lt" => "lessthan",
                "lte" => "lessthanorequal",
                "between" => "range",
                _ => normalized
            };
        }

        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace("[", "[[]", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);
        }
    }
}
