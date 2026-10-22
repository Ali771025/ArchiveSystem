using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using FluentValidation.Results;
using MediatR;

namespace ArchiveSystem.Application.Features.DynamicFields.Queries.SearchCases
{
    public sealed class SearchCasesQuery : IRequest<SearchCasesResultDto>
    {
        public long? ClassificationId { get; set; }
        public bool BypassAuthorizationFilters { get; set; }
        public IReadOnlyCollection<long> AllowedClassificationIds { get; set; } = Array.Empty<long>();
        public bool ExcludeArchivedCasesWhenUnfiltered { get; set; } = true;
        public List<DynamicFilterDto> DynamicFilters { get; set; } = new();
        public string DynamicFilterLogic { get; set; } = "and";
        public List<DynamicFilterGroupDto> DynamicFilterGroups { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "updatedAt";
        public bool SortDescending { get; set; } = true;
        public string? OwnerName { get; set; }
        public string? ExternalCaseNumber { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public IReadOnlyCollection<long> Tags { get; set; } = Array.Empty<long>();
    }

    public sealed class SearchCasesQueryHandler : IRequestHandler<SearchCasesQuery, SearchCasesResultDto>
    {
        private static readonly HashSet<string> AllowedOperators = new(StringComparer.OrdinalIgnoreCase)
        {
            "contains",
            "equals",
            "notequals",
            "lessthan",
            "greaterthan",
            "lessthanorequal",
            "greaterthanorequal",
            "range",
            "eq",
            "neq",
            "gt",
            "gte",
            "lt",
            "lte",
            "in",
            "between",
            "isEmpty"
        };

        private readonly IDynamicTemplateRepository _dynamicTemplateRepository;
        private readonly ICaseDynamicFieldValueRepository _caseDynamicFieldValueRepository;
        private readonly IClassificationRepository _classificationRepository;

        public SearchCasesQueryHandler(
            IDynamicTemplateRepository dynamicTemplateRepository,
            ICaseDynamicFieldValueRepository caseDynamicFieldValueRepository,
            IClassificationRepository classificationRepository)
        {
            _dynamicTemplateRepository = dynamicTemplateRepository;
            _caseDynamicFieldValueRepository = caseDynamicFieldValueRepository;
            _classificationRepository = classificationRepository;
        }

        public async Task<SearchCasesResultDto> Handle(SearchCasesQuery request, CancellationToken cancellationToken)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 200);
            var filterGroups = NormalizeFilterGroups(request);
            var filters = filterGroups.SelectMany(group => group.Filters).ToList();
            var archivedClassificationId = await ResolveArchivedClassificationExclusionAsync(request, cancellationToken);
            var classificationScope = await ResolveClassificationScopeAsync(
                request.ClassificationId,
                cancellationToken);

            if (filters.Count == 0)
            {
                return await _caseDynamicFieldValueRepository.SearchCasesAsync(
                    classificationScope,
                    request.BypassAuthorizationFilters,
                    request.AllowedClassificationIds,
                    archivedClassificationId,
                    NormalizeLogic(request.DynamicFilterLogic),
                    Array.Empty<DynamicFilterGroupDto>(),
                    new Dictionary<Guid, DynamicFieldDefinitionDto>(),
                    request.OwnerName,
                    request.ExternalCaseNumber,
                    request.CreatedBy,
                    request.FromDate,
                    request.ToDate,
                    request.Tags,
                    page,
                    pageSize,
                    request.SortBy,
                    request.SortDescending,
                    cancellationToken);
            }

            var fieldIds = filters.Select(f => f.FieldId).Distinct().ToList();
            var fieldDefinitions = await _dynamicTemplateRepository.GetFieldDefinitionsByIdsAsync(fieldIds, cancellationToken);

            var failures = new List<ValidationFailure>();
            foreach (var filter in filters)
            {
                if (!fieldDefinitions.TryGetValue(filter.FieldId, out var field))
                {
                    failures.Add(new ValidationFailure(nameof(request.DynamicFilterGroups), $"Field '{filter.FieldId}' was not found."));
                    continue;
                }

                if (!field.ShowAsFilter)
                {
                    failures.Add(new ValidationFailure(
                        nameof(request.DynamicFilterGroups),
                        $"Field '{field.Label}' is not enabled for filtering."));
                    continue;
                }

                var op = ResolveOperator(field, filter.Op);
                if (!AllowedOperators.Contains(op))
                {
                    failures.Add(new ValidationFailure(nameof(request.DynamicFilterGroups), $"Operator '{filter.Op}' is not supported."));
                    continue;
                }

                filter.Op = op;
                ValidateOperatorForFieldType(field, op, filter, failures);
            }

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }

            return await _caseDynamicFieldValueRepository.SearchCasesAsync(
                classificationScope,
                request.BypassAuthorizationFilters,
                request.AllowedClassificationIds,
                archivedClassificationId,
                NormalizeLogic(request.DynamicFilterLogic),
                filterGroups,
                fieldDefinitions,
                request.OwnerName,
                request.ExternalCaseNumber,
                request.CreatedBy,
                request.FromDate,
                request.ToDate,
                request.Tags,
                page,
                pageSize,
                request.SortBy,
                request.SortDescending,
                cancellationToken);
        }

        private static List<DynamicFilterGroupDto> NormalizeFilterGroups(SearchCasesQuery request)
        {
            var groups = (request.DynamicFilterGroups ?? new List<DynamicFilterGroupDto>())
                .Where(group => group.Filters.Count > 0)
                .Select(group => new DynamicFilterGroupDto
                {
                    Logic = NormalizeLogic(group.Logic),
                    Filters = group.Filters
                })
                .ToList();

            if (groups.Count == 0 && request.DynamicFilters is { Count: > 0 })
            {
                groups.Add(new DynamicFilterGroupDto
                {
                    Logic = "and",
                    Filters = request.DynamicFilters
                });
            }

            return groups;
        }

        private static string NormalizeLogic(string? logic) =>
            string.Equals(logic?.Trim(), "or", StringComparison.OrdinalIgnoreCase)
                ? "or"
                : "and";

        private async Task<IReadOnlyCollection<long>> ResolveClassificationScopeAsync(
            long? selectedClassificationId,
            CancellationToken cancellationToken)
        {
            if (!selectedClassificationId.HasValue)
            {
                return Array.Empty<long>();
            }

            var classifications = await _classificationRepository.GetAllAsync(cancellationToken);
            var selectedExists = classifications.Any(node => node.Id == selectedClassificationId.Value);
            if (!selectedExists)
            {
                return new[] { selectedClassificationId.Value };
            }

            var childrenByParent = classifications
                .Where(node => node.ParentId.HasValue && node.IsActive)
                .GroupBy(node => node.ParentId!.Value)
                .ToDictionary(group => group.Key, group => group.Select(node => node.Id).ToArray());

            var scope = new HashSet<long> { selectedClassificationId.Value };
            var pending = new Queue<long>();
            pending.Enqueue(selectedClassificationId.Value);

            while (pending.Count > 0)
            {
                var parentId = pending.Dequeue();
                if (!childrenByParent.TryGetValue(parentId, out var childIds))
                {
                    continue;
                }

                foreach (var childId in childIds)
                {
                    if (scope.Add(childId))
                    {
                        pending.Enqueue(childId);
                    }
                }
            }

            return scope;
        }

        private async Task<long?> ResolveArchivedClassificationExclusionAsync(
            SearchCasesQuery request,
            CancellationToken cancellationToken)
        {
            if (!request.ExcludeArchivedCasesWhenUnfiltered || request.ClassificationId.HasValue)
            {
                return null;
            }

            var archivedClassification = await _classificationRepository.GetBySystemKeyAsync(
                SystemClassificationKeys.ArchivedCases,
                cancellationToken);

            return archivedClassification?.Id;
        }

        private static string ResolveOperator(DynamicFieldDefinitionDto field, string? requestedOperator)
        {
            var normalizedRequested = NormalizeOperatorToken(requestedOperator);
            if (!string.IsNullOrWhiteSpace(normalizedRequested))
            {
                return normalizedRequested;
            }

            return field.FilterType switch
            {
                DynamicFieldFilterType.Contains => "contains",
                DynamicFieldFilterType.Equals => "equals",
                DynamicFieldFilterType.NotEquals => "notequals",
                DynamicFieldFilterType.LessThan => "lessthan",
                DynamicFieldFilterType.GreaterThan => "greaterthan",
                DynamicFieldFilterType.LessThanOrEqual => "lessthanorequal",
                DynamicFieldFilterType.GreaterThanOrEqual => "greaterthanorequal",
                DynamicFieldFilterType.Range => "range",
                _ => "contains"
            };
        }

        private static string NormalizeOperatorToken(string? op)
        {
            var normalized = op?.Trim().ToLowerInvariant() ?? string.Empty;
            return normalized switch
            {
                "" => string.Empty,
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

        private static void ValidateOperatorForFieldType(
            DynamicFieldDefinitionDto field,
            string op,
            DynamicFilterDto filter,
            ICollection<ValidationFailure> failures)
        {
            var type = field.FieldType;
            var hasSingleValue = filter.Value.HasValue && filter.Value.Value.ValueKind != System.Text.Json.JsonValueKind.Null;
            var hasFromValue = filter.FromValue.HasValue && filter.FromValue.Value.ValueKind != System.Text.Json.JsonValueKind.Null;
            var hasToValue = filter.ToValue.HasValue && filter.ToValue.Value.ValueKind != System.Text.Json.JsonValueKind.Null;
            var hasManyValues = filter.Values != null && filter.Values.Count > 0;

            if (!op.Equals("isempty", StringComparison.OrdinalIgnoreCase)
                && !op.Equals("range", StringComparison.OrdinalIgnoreCase)
                && !hasSingleValue
                && !hasManyValues)
            {
                failures.Add(new ValidationFailure(nameof(filter.Value), $"Operator '{op}' requires a value."));
                return;
            }

            if (op.Equals("contains", StringComparison.OrdinalIgnoreCase)
                && type is not (DynamicFieldType.String or DynamicFieldType.MultiLineText))
            {
                failures.Add(new ValidationFailure(nameof(filter.Op), "'contains' is supported only for string fields."));
            }

            if ((op.Equals("greaterthan", StringComparison.OrdinalIgnoreCase)
                || op.Equals("greaterthanorequal", StringComparison.OrdinalIgnoreCase)
                || op.Equals("lessthan", StringComparison.OrdinalIgnoreCase)
                || op.Equals("lessthanorequal", StringComparison.OrdinalIgnoreCase)
                || op.Equals("range", StringComparison.OrdinalIgnoreCase))
                && type is not (DynamicFieldType.Number or DynamicFieldType.Decimal or DynamicFieldType.Date))
            {
                failures.Add(new ValidationFailure(nameof(filter.Op), $"Operator '{op}' is supported only for number/date fields."));
            }

            if (op.Equals("in", StringComparison.OrdinalIgnoreCase))
            {
                var inAllowed = type is DynamicFieldType.String or DynamicFieldType.MultiLineText or DynamicFieldType.Number or DynamicFieldType.Decimal
                                || type is DynamicFieldType.Enum or DynamicFieldType.MultiSelect;
                if (!inAllowed)
                {
                    failures.Add(new ValidationFailure(nameof(filter.Op), "'in' is supported only for enum, string, or number fields."));
                }
            }

            if (op.Equals("range", StringComparison.OrdinalIgnoreCase))
            {
                var hasExplicitRange = hasFromValue && hasToValue;
                var hasArrayRange = filter.Values != null && filter.Values.Count == 2;

                if (!hasExplicitRange && !hasArrayRange)
                {
                    failures.Add(new ValidationFailure(nameof(filter.Values), "'range' requires both FromValue and ToValue, or exactly two values in Values."));
                }
            }
        }
    }
}
