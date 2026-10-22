using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Application.Features.Search.Documents
{
    public sealed class AdvancedDocumentSearchRequest
    {
        public string? GeneralQuery { get; set; }
        public string? DocumentName { get; set; }
        public string? CaseNumber { get; set; }
        public string? OwnerName { get; set; }
        public string? BankName { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<DynamicFilterDto> Filters { get; set; } = new();
        public bool IncludeDeleted { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string SortBy { get; set; } = "modifiedAt";
        public string SortDirection { get; set; } = "desc";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public sealed class DynamicFilterDto
    {
        public string Source { get; set; } = "dossier";
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = "Contains";
        public string? Value { get; set; }
        public string? ValueTo { get; set; }
        public List<string> Values { get; set; } = new();
        public string? ValueType { get; set; }
    }

    public sealed class DocumentSearchResultDto
    {
        public long DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DossierNumber { get; set; } = string.Empty;
        public string Owners { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public DateTime ModifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }

    public sealed class AdvancedDocumentSearchResponseDto
    {
        public List<DocumentSearchResultDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public sealed class AdvancedDocumentSearchQuery : IRequest<AdvancedDocumentSearchResponseDto>
    {
        public AdvancedDocumentSearchRequest Request { get; init; } = new();
        public IReadOnlyCollection<long> AllowedClassificationIds { get; init; } = Array.Empty<long>();
        public bool BypassAuthorizationFilters { get; init; }
    }

    public sealed class AdvancedDocumentSearchQueryHandler
        : IRequestHandler<AdvancedDocumentSearchQuery, AdvancedDocumentSearchResponseDto>
    {
        private readonly IArchiveDbContext _dbContext;

        public AdvancedDocumentSearchQueryHandler(IArchiveDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AdvancedDocumentSearchResponseDto> Handle(
            AdvancedDocumentSearchQuery request,
            CancellationToken cancellationToken)
        {
            var pageNumber = request.Request.PageNumber < 1 ? 1 : request.Request.PageNumber;
            var pageSize = request.Request.PageSize < 1 ? 20 : Math.Min(request.Request.PageSize, 200);
            var allowedClassifications = request.AllowedClassificationIds.Distinct().ToArray();
            var bypassAuthorizationFilters = request.BypassAuthorizationFilters;

            if (!bypassAuthorizationFilters && allowedClassifications.Length == 0)
            {
                return new AdvancedDocumentSearchResponseDto
                {
                    Items = new List<DocumentSearchResultDto>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                };
            }

            var query = from file in _dbContext.Files.AsNoTracking()
                        join dossier in _dbContext.Cases.AsNoTracking() on file.CaseId equals dossier.Id into dossierJoin
                        from dossier in dossierJoin.DefaultIfEmpty()
                        where dossier != null
                            && (bypassAuthorizationFilters || allowedClassifications.Contains(dossier.ClassificationId))
                        select new DocumentSearchProjection
                        {
                            FileId = file.Id,
                            ClassificationId = dossier.ClassificationId,
                            FileName = file.Name,
                            ContentType = file.ContentType,
                            Extension = file.Extension.Value,
                            FileDeletedAt = file.DeletedAt,
                            FileCreatedAt = file.CreatedAt,
                            FileModifiedAt = file.ModifiedAt,
                            DossierNumber = dossier.CaseNumber,
                            DossierTitle = dossier.Title,
                            Owners = dossier.Owners,
                            BankName = dossier.Location,
                            DossierDeletedAt = dossier.DeletedAt,
                            CaseId = dossier.Id,
                        };

            if (!request.Request.IncludeDeleted)
            {
                query = query.Where(x => x.FileDeletedAt == null && x.DossierDeletedAt == null);
            }

            if (!string.IsNullOrWhiteSpace(request.Request.GeneralQuery))
            {
                var term = request.Request.GeneralQuery.Trim();
                query = query.Where(x =>
                    EF.Functions.Like(x.FileName, $"%{EscapeLike(term)}%") ||
                    EF.Functions.Like(x.DossierNumber, $"%{EscapeLike(term)}%") ||
                    EF.Functions.Like(x.DossierTitle, $"%{EscapeLike(term)}%") ||
                    EF.Functions.Like(x.Owners, $"%{EscapeLike(term)}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.Request.DocumentName))
            {
                var documentName = request.Request.DocumentName.Trim();
                query = query.Where(x => EF.Functions.Like(x.FileName, $"%{EscapeLike(documentName)}%"));
            }

            if (!string.IsNullOrWhiteSpace(request.Request.CaseNumber))
            {
                var caseNumber = request.Request.CaseNumber.Trim();
                var caseNumberPattern = $"%{EscapeLike(caseNumber)}%";
                var withPrefixPattern = $"%{EscapeLike($"A-{caseNumber}")}%";
                var withoutPrefix = caseNumber.StartsWith("A-", StringComparison.OrdinalIgnoreCase)
                    ? caseNumber[2..]
                    : caseNumber;
                var withoutPrefixPattern = $"%{EscapeLike(withoutPrefix)}%";

                query = query.Where(x =>
                    EF.Functions.Like(x.DossierNumber, caseNumberPattern)
                    || EF.Functions.Like(x.DossierNumber, withPrefixPattern)
                    || EF.Functions.Like(x.DossierNumber, withoutPrefixPattern));
            }

            if (!string.IsNullOrWhiteSpace(request.Request.OwnerName))
            {
                var ownerName = request.Request.OwnerName.Trim();
                var ownerNameVariants = BuildTextVariants(ownerName);
                var variantPatterns = ownerNameVariants
                    .Select(variant => $"%{EscapeLike(variant)}%")
                    .ToArray();

                var ownerTokens = ownerName
                    .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(4)
                    .ToArray();

                if (ownerTokens.Length > 1)
                {
                    foreach (var token in ownerTokens)
                    {
                        var tokenPatterns = BuildTextVariants(token)
                            .Select(variant => $"%{EscapeLike(variant)}%")
                            .Distinct(StringComparer.Ordinal)
                            .ToArray();

                        query = ApplyOwnersLikePatterns(query, tokenPatterns);
                    }
                }
                else
                {
                    query = ApplyOwnersLikePatterns(query, variantPatterns);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Request.BankName))
            {
                var bankName = request.Request.BankName.Trim();
                query = query.Where(x => EF.Functions.Like(x.BankName ?? string.Empty, $"%{EscapeLike(bankName)}%"));
            }

            if (request.Request.CreatedFrom.HasValue)
            {
                query = query.Where(x => x.FileCreatedAt >= request.Request.CreatedFrom.Value);
            }

            if (request.Request.CreatedTo.HasValue)
            {
                var createdToExclusive = request.Request.CreatedTo.Value.Date.AddDays(1);
                query = query.Where(x => x.FileCreatedAt < createdToExclusive);
            }

            if (request.Request.Tags.Count > 0)
            {
                var normalizedTags = request.Request.Tags
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => tag.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (normalizedTags.Length > 0)
                {
                    query = query.Where(x => _dbContext.FileTags
                        .Where(link => link.FileId == x.FileId)
                        .Select(link => link.Tag.Name)
                        .Any(tagName => normalizedTags.Contains(tagName)));
                }
            }

            if (request.Request.Filters.Count > 0)
            {
                foreach (var filter in request.Request.Filters.Where(filter => !string.IsNullOrWhiteSpace(filter.Field)))
                {
                    query = ApplyDynamicFilter(query, filter);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);
            query = ApplySort(query, request.Request.SortBy, request.Request.SortDirection);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new DocumentSearchResultDto
                {
                    DocumentId = x.FileId,
                    DocumentName = x.FileName,
                    DossierNumber = x.DossierNumber,
                    Owners = x.Owners,
                    BankName = x.BankName,
                    ModifiedAt = x.FileModifiedAt ?? x.FileCreatedAt,
                    CreatedAt = x.FileCreatedAt,
                    ContentType = x.ContentType,
                    Extension = x.Extension,
                    IsDeleted = x.FileDeletedAt.HasValue || x.DossierDeletedAt.HasValue,
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.Owners = string.Join("، ", OwnerDisplayNameParser.ParseNames(item.Owners));
            }

            return new AdvancedDocumentSearchResponseDto
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };
        }

        private IQueryable<DocumentSearchProjection> ApplyDynamicFilter(
            IQueryable<DocumentSearchProjection> query,
            DynamicFilterDto filter)
        {
            var source = (filter.Source ?? string.Empty).Trim().ToLowerInvariant();
            var field = (filter.Field ?? string.Empty).Trim().ToLowerInvariant();
            var op = (filter.Operator ?? "contains").Trim().ToLowerInvariant();

            if (source == "dynamicfield")
            {
                var value = filter.Value?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                {
                    return query;
                }

                return query.Where(x => _dbContext.CaseFieldValueStrings
                    .Where(dynamicValue => dynamicValue.CaseId == x.CaseId)
                    .Any(dynamicValue =>
                        dynamicValue.Field.Key == field &&
                        EF.Functions.Like(dynamicValue.Value, $"%{EscapeLike(value)}%")));
            }

            var filterValue = filter.Value?.Trim() ?? string.Empty;
            var filterValues = (filter.Values ?? new List<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList();

            var fieldKey = (source, field) switch
            {
                ("document", "name") or ("document", "filename") => "fileName",
                ("document", "contenttype") => "contentType",
                ("dossier", "casenumber") or ("dossier", "dossiernumber") => "dossierNumber",
                ("dossier", "classificationid") => "classificationId",
                ("dossier", "ownername") or ("owner", "fullname") => "owners",
                ("dossier", "bankname") => "bankName",
                _ => null,
            };

            if (fieldKey is null)
            {
                return query;
            }

            if (fieldKey == "classificationId")
            {
                return ApplyClassificationIdFilter(query, op, filterValue, filterValues);
            }

            return ApplyStringFilter(query, fieldKey, op, filterValue, filter.ValueTo);
        }

        private static IQueryable<DocumentSearchProjection> ApplyClassificationIdFilter(
            IQueryable<DocumentSearchProjection> query,
            string op,
            string value,
            IReadOnlyCollection<string> values)
        {
            if (op == "isnull")
            {
                return query.Where(x => x.ClassificationId == 0);
            }

            if (op == "isnotnull")
            {
                return query.Where(x => x.ClassificationId != 0);
            }

            if (op is "in" or "notin")
            {
                var ids = values
                    .Select(raw => long.TryParse(raw, out var parsed) ? parsed : (long?)null)
                    .Where(parsed => parsed.HasValue)
                    .Select(parsed => parsed!.Value)
                    .Distinct()
                    .ToArray();

                if (ids.Length == 0 && long.TryParse(value, out var fallbackId))
                {
                    ids = new[] { fallbackId };
                }

                if (ids.Length == 0)
                {
                    return query;
                }

                return op == "in"
                    ? query.Where(x => ids.Contains(x.ClassificationId))
                    : query.Where(x => !ids.Contains(x.ClassificationId));
            }

            if (!long.TryParse(value, out var classificationId))
            {
                return query;
            }

            return op switch
            {
                "equals" => query.Where(x => x.ClassificationId == classificationId),
                "notequals" => query.Where(x => x.ClassificationId != classificationId),
                "greaterthan" => query.Where(x => x.ClassificationId > classificationId),
                "greaterthanorequal" => query.Where(x => x.ClassificationId >= classificationId),
                "lessthan" => query.Where(x => x.ClassificationId < classificationId),
                "lessthanorequal" => query.Where(x => x.ClassificationId <= classificationId),
                _ => query,
            };
        }

        private static IQueryable<DocumentSearchProjection> ApplyStringFilter(
            IQueryable<DocumentSearchProjection> query,
            string fieldKey,
            string op,
            string value,
            string? valueTo)
        {
            if (string.IsNullOrWhiteSpace(value) && op is not "isnull" and not "isnotnull")
            {
                return query;
            }

            return fieldKey switch
            {
                "fileName" => ApplyStringFilterForFileName(query, op, value, valueTo),
                "contentType" => ApplyStringFilterForContentType(query, op, value, valueTo),
                "dossierNumber" => ApplyStringFilterForDossierNumber(query, op, value, valueTo),
                "owners" => ApplyStringFilterForOwners(query, op, value, valueTo),
                "bankName" => ApplyStringFilterForBankName(query, op, value, valueTo),
                _ => query,
            };
        }

        private static IQueryable<DocumentSearchProjection> ApplySort(
            IQueryable<DocumentSearchProjection> query,
            string? sortBy,
            string? sortDirection)
        {
            var key = (sortBy ?? "modifiedAt").Trim().ToLowerInvariant();
            var desc = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return (key, desc) switch
            {
                ("documentname", true) => query.OrderByDescending(x => x.FileName),
                ("documentname", false) => query.OrderBy(x => x.FileName),
                ("dossiernumber", true) => query.OrderByDescending(x => x.DossierNumber),
                ("dossiernumber", false) => query.OrderBy(x => x.DossierNumber),
                ("ownername", true) => query.OrderByDescending(x => x.Owners),
                ("ownername", false) => query.OrderBy(x => x.Owners),
                ("createdat", true) => query.OrderByDescending(x => x.FileCreatedAt),
                ("createdat", false) => query.OrderBy(x => x.FileCreatedAt),
                (_, true) => query.OrderByDescending(x => x.FileModifiedAt ?? x.FileCreatedAt),
                (_, false) => query.OrderBy(x => x.FileModifiedAt ?? x.FileCreatedAt),
            };
        }

        private static string EscapeLike(string input)
        {
            return input
                .Replace("[", "[[]", StringComparison.Ordinal)
                .Replace("%", "[%]", StringComparison.Ordinal)
                .Replace("_", "[_]", StringComparison.Ordinal);
        }

        private static List<string> BuildTextVariants(string value)
        {
            var normalized = value.Trim();
            if (normalized.Length == 0)
            {
                return new List<string>();
            }

            var variants = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                normalized
            };

            variants.Add(normalized.Replace('ي', 'ی').Replace('ك', 'ک'));
            variants.Add(normalized.Replace('ی', 'ي').Replace('ک', 'ك'));

            return variants
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Take(4)
                .ToList();
        }

        private static IQueryable<DocumentSearchProjection> ApplyStringFilterForFileName(
            IQueryable<DocumentSearchProjection> query,
            string op,
            string value,
            string? valueTo)
        {
            return op switch
            {
                "equals" => query.Where(x => x.FileName == value),
                "notequals" => query.Where(x => x.FileName != value),
                "contains" => query.Where(x => x.FileName.Contains(value)),
                "startswith" => query.Where(x => x.FileName.StartsWith(value)),
                "endswith" => query.Where(x => x.FileName.EndsWith(value)),
                "isnull" => query.Where(x => x.FileName == null),
                "isnotnull" => query.Where(x => x.FileName != null),
                "between" when !string.IsNullOrWhiteSpace(valueTo) => query.Where(x => string.Compare(x.FileName, value, StringComparison.Ordinal) >= 0 && string.Compare(x.FileName, valueTo, StringComparison.Ordinal) <= 0),
                _ => query,
            };
        }

        private static IQueryable<DocumentSearchProjection> ApplyStringFilterForContentType(
            IQueryable<DocumentSearchProjection> query,
            string op,
            string value,
            string? valueTo)
        {
            return op switch
            {
                "equals" => query.Where(x => x.ContentType == value),
                "notequals" => query.Where(x => x.ContentType != value),
                "contains" => query.Where(x => x.ContentType.Contains(value)),
                "startswith" => query.Where(x => x.ContentType.StartsWith(value)),
                "endswith" => query.Where(x => x.ContentType.EndsWith(value)),
                "isnull" => query.Where(x => x.ContentType == null),
                "isnotnull" => query.Where(x => x.ContentType != null),
                "between" when !string.IsNullOrWhiteSpace(valueTo) => query.Where(x => string.Compare(x.ContentType, value, StringComparison.Ordinal) >= 0 && string.Compare(x.ContentType, valueTo, StringComparison.Ordinal) <= 0),
                _ => query,
            };
        }

        private static IQueryable<DocumentSearchProjection> ApplyStringFilterForDossierNumber(
            IQueryable<DocumentSearchProjection> query,
            string op,
            string value,
            string? valueTo)
        {
            return op switch
            {
                "equals" => query.Where(x => x.DossierNumber == value),
                "notequals" => query.Where(x => x.DossierNumber != value),
                "contains" => query.Where(x => x.DossierNumber.Contains(value)),
                "startswith" => query.Where(x => x.DossierNumber.StartsWith(value)),
                "endswith" => query.Where(x => x.DossierNumber.EndsWith(value)),
                "isnull" => query.Where(x => x.DossierNumber == null),
                "isnotnull" => query.Where(x => x.DossierNumber != null),
                "between" when !string.IsNullOrWhiteSpace(valueTo) => query.Where(x => string.Compare(x.DossierNumber, value, StringComparison.Ordinal) >= 0 && string.Compare(x.DossierNumber, valueTo, StringComparison.Ordinal) <= 0),
                _ => query,
            };
        }

        private static IQueryable<DocumentSearchProjection> ApplyStringFilterForOwners(
            IQueryable<DocumentSearchProjection> query,
            string op,
            string value,
            string? valueTo)
        {
            return op switch
            {
                "equals" => query.Where(x => x.Owners == value),
                "notequals" => query.Where(x => x.Owners != value),
                "contains" => query.Where(x => x.Owners.Contains(value)),
                "startswith" => query.Where(x => x.Owners.StartsWith(value)),
                "endswith" => query.Where(x => x.Owners.EndsWith(value)),
                "isnull" => query.Where(x => x.Owners == null),
                "isnotnull" => query.Where(x => x.Owners != null),
                "between" when !string.IsNullOrWhiteSpace(valueTo) => query.Where(x => string.Compare(x.Owners, value, StringComparison.Ordinal) >= 0 && string.Compare(x.Owners, valueTo, StringComparison.Ordinal) <= 0),
                _ => query,
            };
        }

        private static IQueryable<DocumentSearchProjection> ApplyOwnersLikePatterns(
            IQueryable<DocumentSearchProjection> query,
            IReadOnlyList<string> patterns)
        {
            return patterns.Count switch
            {
                0 => query,
                1 => query.Where(x => EF.Functions.Like(x.Owners, patterns[0])),
                2 => query.Where(x =>
                    EF.Functions.Like(x.Owners, patterns[0]) ||
                    EF.Functions.Like(x.Owners, patterns[1])),
                3 => query.Where(x =>
                    EF.Functions.Like(x.Owners, patterns[0]) ||
                    EF.Functions.Like(x.Owners, patterns[1]) ||
                    EF.Functions.Like(x.Owners, patterns[2])),
                _ => query.Where(x =>
                    EF.Functions.Like(x.Owners, patterns[0]) ||
                    EF.Functions.Like(x.Owners, patterns[1]) ||
                    EF.Functions.Like(x.Owners, patterns[2]) ||
                    EF.Functions.Like(x.Owners, patterns[3]))
            };
        }

        private static IQueryable<DocumentSearchProjection> ApplyStringFilterForBankName(
            IQueryable<DocumentSearchProjection> query,
            string op,
            string value,
            string? valueTo)
        {
            return op switch
            {
                "equals" => query.Where(x => (x.BankName ?? string.Empty) == value),
                "notequals" => query.Where(x => (x.BankName ?? string.Empty) != value),
                "contains" => query.Where(x => (x.BankName ?? string.Empty).Contains(value)),
                "startswith" => query.Where(x => (x.BankName ?? string.Empty).StartsWith(value)),
                "endswith" => query.Where(x => (x.BankName ?? string.Empty).EndsWith(value)),
                "isnull" => query.Where(x => x.BankName == null),
                "isnotnull" => query.Where(x => x.BankName != null),
                "between" when !string.IsNullOrWhiteSpace(valueTo) => query.Where(x => string.Compare(x.BankName ?? string.Empty, value, StringComparison.Ordinal) >= 0 && string.Compare(x.BankName ?? string.Empty, valueTo, StringComparison.Ordinal) <= 0),
                _ => query,
            };
        }

        private sealed class DocumentSearchProjection
        {
            public long FileId { get; init; }
            public long CaseId { get; init; }
            public long ClassificationId { get; init; }
            public string FileName { get; init; } = string.Empty;
            public string ContentType { get; init; } = string.Empty;
            public string Extension { get; init; } = string.Empty;
            public DateTime FileCreatedAt { get; init; }
            public DateTime? FileModifiedAt { get; init; }
            public DateTime? FileDeletedAt { get; init; }
            public string DossierNumber { get; init; } = string.Empty;
            public string DossierTitle { get; init; } = string.Empty;
            public string Owners { get; init; } = string.Empty;
            public string? BankName { get; init; }
            public DateTime? DossierDeletedAt { get; init; }
        }
    }
}
