using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Dossiers.AdvancedSearch;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Enums;
using FluentValidation.Results;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public sealed class EfDossierAdvancedSearchRepository : IDossierAdvancedSearchRepository
    {
        private readonly ArchiveDbContext _context;
        private bool? _hasLegacyDynamicFieldTables;

        public EfDossierAdvancedSearchRepository(ArchiveDbContext context)
        {
            _context = context;
        }

        public async Task<DossierAdvancedSearchFieldCatalogDto> GetAvailableFieldsAsync(
            IReadOnlyCollection<long> allowedClassificationIds,
            long? classificationId = null,
            CancellationToken cancellationToken = default)
        {
            var allowedSet = allowedClassificationIds.Distinct().ToArray();
            if (allowedSet.Length == 0)
            {
                return new DossierAdvancedSearchFieldCatalogDto();
            }

            if (classificationId.HasValue && !allowedSet.Contains(classificationId.Value))
            {
                return new DossierAdvancedSearchFieldCatalogDto();
            }

            var fields = BuildFixedFieldCatalog();

            if (!await HasLegacyDynamicFieldTablesAsync(cancellationToken))
            {
                return new DossierAdvancedSearchFieldCatalogDto
                {
                    Fields = fields
                };
            }

            try
            {
                var dynamicFieldsQuery = _context.DossierDynamicFields
                    .AsNoTracking()
                    .Where(field =>
                        field.DeletedAt == null &&
                        field.IsActive &&
                        (field.Scope == DossierDynamicFieldScope.Public ||
                         (field.Scope == DossierDynamicFieldScope.ClassificationSpecific &&
                          field.ClassificationId.HasValue &&
                          allowedSet.Contains(field.ClassificationId.Value))));

                if (classificationId.HasValue)
                {
                    dynamicFieldsQuery = dynamicFieldsQuery
                        .Where(field =>
                            field.Scope == DossierDynamicFieldScope.Public ||
                            field.ClassificationId == classificationId.Value);
                }

                var dynamicFields = await dynamicFieldsQuery
                    .OrderBy(field => field.Scope)
                    .ThenBy(field => field.ClassificationId)
                    .ThenBy(field => field.DisplayOrder)
                    .ThenBy(field => field.Title)
                    .Select(field => new
                    {
                        field.Id,
                        field.Title,
                        field.ApiName,
                        field.FieldType,
                        field.ClassificationId,
                        Options = field.Options
                            .Where(option => option.IsActive)
                            .OrderBy(option => option.DisplayOrder)
                            .Select(option => new { option.Value, option.Label })
                            .ToList()
                    })
                    .ToListAsync(cancellationToken);

                fields.AddRange(dynamicFields.Select(field => new DossierSearchFieldDescriptorDto
                {
                    Key = $"dynamic.{field.ApiName}",
                    Title = field.Title,
                    Source = DossierSearchFieldSource.DynamicField,
                    Field = field.ApiName,
                    ValueType = MapDynamicValueType(field.FieldType),
                    IsNullable = true,
                    SupportedOperators = GetSupportedOperatorsForDynamicField(field.FieldType),
                    DynamicFieldApiName = field.ApiName,
                    DynamicFieldType = field.FieldType,
                    DynamicFieldId = field.Id,
                    ClassificationId = field.ClassificationId,
                    Options = field.Options
                        .Select(option => new DossierSearchFieldOptionDto
                        {
                            Value = option.Value,
                            Label = option.Label
                        })
                        .ToList()
                }));
            }
            catch (SqlException ex) when (IsMissingLegacyDossierDynamicSchema(ex))
            {
                // Some deployments have only case-based dynamic fields and no legacy dossier dynamic tables.
                // In that shape, advanced-search fixed fields should still load instead of returning 500.
            }

            return new DossierAdvancedSearchFieldCatalogDto
            {
                Fields = fields
            };
        }

        private async Task<bool> HasLegacyDynamicFieldTablesAsync(CancellationToken cancellationToken)
        {
            if (_hasLegacyDynamicFieldTables.HasValue)
            {
                return _hasLegacyDynamicFieldTables.Value;
            }

            var hasDynamicFields = await _context.Database
                .SqlQueryRaw<int>(
                    "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DossierDynamicFields') THEN 1 ELSE 0 END AS int) AS [Value]")
                .SingleAsync(cancellationToken) == 1;

            if (!hasDynamicFields)
            {
                _hasLegacyDynamicFieldTables = false;
                return false;
            }

            var hasDynamicFieldOptions = await _context.Database
                .SqlQueryRaw<int>(
                    "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DossierDynamicFieldOptions') THEN 1 ELSE 0 END AS int) AS [Value]")
                .SingleAsync(cancellationToken) == 1;

            _hasLegacyDynamicFieldTables = hasDynamicFieldOptions;
            return hasDynamicFieldOptions;
        }

        private static bool IsMissingLegacyDossierDynamicSchema(SqlException ex)
        {
            if (ex.Number != 208)
            {
                return false;
            }

            return ex.Message.Contains("DossierDynamicFields", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("DossierDynamicFieldOptions", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<DossierAdvancedSearchResultDto> SearchAsync(
            DossierAdvancedSearchRequestDto request,
            IReadOnlyCollection<long> allowedClassificationIds,
            CancellationToken cancellationToken = default)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 200);
            var allowedSet = allowedClassificationIds.Distinct().ToArray();
            if (allowedSet.Length == 0)
            {
                return new DossierAdvancedSearchResultDto
                {
                    Items = new List<DossierAdvancedSearchItemDto>(),
                    TotalCount = 0,
                    Page = page,
                    PageSize = pageSize
                };
            }

            IQueryable<Dossier> query = _context.Dossiers
                .AsNoTracking()
                .Where(d => allowedSet.Contains(d.ClassificationId));

            if (!request.IncludeDeleted)
            {
                query = query.Where(d => d.DeletedAt == null);
            }

            var predicate = new DossierSearchExpressionBuilder().Build(request.Root);
            query = query.Where(predicate);

            var totalCount = await query.CountAsync(cancellationToken);
            query = ApplySort(query, request.SortBy, request.SortDirection);

            var skip = (page - 1) * pageSize;
            var dossiers = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(d => new
                {
                    d.Id,
                    d.DossierNumber,
                    d.ExternalCaseNumber,
                    d.Address,
                    d.UsageType,
                    d.Latitude,
                    d.Longitude,
                    d.ClassificationId,
                    d.CreatedAt,
                    d.ModifiedAt,
                    d.DeletedAt
                })
                .ToListAsync(cancellationToken);

            var dossierIds = dossiers.Select(d => d.Id).ToArray();
            var ownerNames = await _context.DossierOwners
                .AsNoTracking()
                .Where(o => dossierIds.Contains(o.DossierId) && o.DeletedAt == null)
                .Select(o => new
                {
                    o.DossierId,
                    Name = o.Owner.OwnerType == OwnerType.Natural
                        ? ((o.Owner.FirstName ?? string.Empty) + " " + (o.Owner.LastName ?? string.Empty)).Trim()
                        : (o.Owner.CompanyName ?? string.Empty)
                })
                .ToListAsync(cancellationToken);

            var ownersLookup = ownerNames
                .GroupBy(x => x.DossierId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(x => x.Name.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList());

            return new DossierAdvancedSearchResultDto
            {
                Items = dossiers.Select(d => new DossierAdvancedSearchItemDto
                {
                    Id = d.Id,
                    DossierNumber = d.DossierNumber,
                    ExternalCaseNumber = d.ExternalCaseNumber,
                    Address = d.Address,
                    UsageType = d.UsageType,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    ClassificationId = d.ClassificationId,
                    CreatedAt = d.CreatedAt,
                    ModifiedAt = d.ModifiedAt,
                    IsDeleted = d.DeletedAt.HasValue,
                    OwnerNames = ownersLookup.TryGetValue(d.Id, out var names) ? names : new List<string>()
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static IQueryable<Dossier> ApplySort(IQueryable<Dossier> query, string? sortBy, string? sortDirection)
        {
            var sortKey = (sortBy ?? "modifiedAt").Trim().ToLowerInvariant();
            var isDescending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

            return sortKey switch
            {
                "dossiernumber" => isDescending ? query.OrderByDescending(d => d.DossierNumber) : query.OrderBy(d => d.DossierNumber),
                "externalcasenumber" => isDescending ? query.OrderByDescending(d => d.ExternalCaseNumber) : query.OrderBy(d => d.ExternalCaseNumber),
                "address" => isDescending ? query.OrderByDescending(d => d.Address) : query.OrderBy(d => d.Address),
                "usagetype" => isDescending ? query.OrderByDescending(d => d.UsageType) : query.OrderBy(d => d.UsageType),
                "createdat" => isDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
                _ => isDescending ? query.OrderByDescending(d => d.ModifiedAt ?? d.CreatedAt) : query.OrderBy(d => d.ModifiedAt ?? d.CreatedAt)
            };
        }

        private static List<DossierSearchFieldDescriptorDto> BuildFixedFieldCatalog()
        {
            return new List<DossierSearchFieldDescriptorDto>
            {
                new()
                {
                    Key = "dossier.DossierNumber",
                    Title = "شماره پرونده",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "DossierNumber",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = false,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "dossier.ExternalCaseNumber",
                    Title = "شماره پرونده خارجی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "ExternalCaseNumber",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = false,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "dossier.Address",
                    Title = "نشانی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "Address",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = false,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "dossier.DefaultImagePath",
                    Title = "مسیر تصویر پیش‌فرض",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "DefaultImagePath",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "dossier.RenovationCode",
                    Title = "کد نوسازی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "RenovationCode",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "dossier.PostalCode",
                    Title = "کد پستی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "PostalCode",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "dossier.RegistrationPlate",
                    Title = "پلاک ثبتی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "RegistrationPlate",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "dossier.UsageType",
                    Title = "نوع کاربری",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "UsageType",
                    ValueType = DossierSearchValueType.Integer,
                    IsNullable = false,
                    SupportedOperators = GetEnumOperators()
                },
                new()
                {
                    Key = "dossier.Latitude",
                    Title = "عرض جغرافیایی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "Latitude",
                    ValueType = DossierSearchValueType.Decimal,
                    IsNullable = true,
                    SupportedOperators = GetNullableComparableOperators()
                },
                new()
                {
                    Key = "dossier.Longitude",
                    Title = "طول جغرافیایی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "Longitude",
                    ValueType = DossierSearchValueType.Decimal,
                    IsNullable = true,
                    SupportedOperators = GetNullableComparableOperators()
                },
                new()
                {
                    Key = "dossier.ClassificationId",
                    Title = "دسته‌بندی",
                    Source = DossierSearchFieldSource.Dossier,
                    Field = "ClassificationId",
                    ValueType = DossierSearchValueType.Integer,
                    IsNullable = false,
                    SupportedOperators = GetComparableOperators()
                },
                new()
                {
                    Key = "owner.FullName",
                    Title = "نام کامل مالک",
                    Source = DossierSearchFieldSource.Owner,
                    Field = "FullName",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "owner.NationalCode",
                    Title = "کد ملی مالک",
                    Source = DossierSearchFieldSource.Owner,
                    Field = "NationalCode",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "owner.NationalId",
                    Title = "شناسه ملی مالک",
                    Source = DossierSearchFieldSource.Owner,
                    Field = "NationalId",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "owner.Mobile",
                    Title = "شماره همراه مالک",
                    Source = DossierSearchFieldSource.Owner,
                    Field = "Mobile",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "owner.HomePhone",
                    Title = "تلفن ثابت مالک",
                    Source = DossierSearchFieldSource.Owner,
                    Field = "HomePhone",
                    ValueType = DossierSearchValueType.Text,
                    IsNullable = true,
                    SupportedOperators = GetStringOperators()
                },
                new()
                {
                    Key = "owner.OwnerType",
                    Title = "نوع مالک",
                    Source = DossierSearchFieldSource.Owner,
                    Field = "OwnerType",
                    ValueType = DossierSearchValueType.Integer,
                    IsNullable = false,
                    SupportedOperators = GetEnumOperators()
                }
            };
        }

        private static DossierSearchValueType MapDynamicValueType(DossierDynamicFieldType fieldType)
        {
            return fieldType switch
            {
                DossierDynamicFieldType.Text => DossierSearchValueType.Text,
                DossierDynamicFieldType.MultilineText => DossierSearchValueType.Text,
                DossierDynamicFieldType.Dropdown => DossierSearchValueType.Text,
                DossierDynamicFieldType.Integer => DossierSearchValueType.Integer,
                DossierDynamicFieldType.Decimal => DossierSearchValueType.Decimal,
                DossierDynamicFieldType.Date => DossierSearchValueType.Date,
                DossierDynamicFieldType.DateTime => DossierSearchValueType.DateTime,
                DossierDynamicFieldType.Time => DossierSearchValueType.Time,
                DossierDynamicFieldType.Checkbox => DossierSearchValueType.Boolean,
                _ => DossierSearchValueType.Text
            };
        }

        private static List<DossierComparisonOperator> GetSupportedOperatorsForDynamicField(DossierDynamicFieldType fieldType)
        {
            return fieldType switch
            {
                DossierDynamicFieldType.Text or DossierDynamicFieldType.MultilineText or DossierDynamicFieldType.Dropdown
                    => GetStringOperators(),
                DossierDynamicFieldType.Integer or DossierDynamicFieldType.Decimal or DossierDynamicFieldType.Date or DossierDynamicFieldType.DateTime or DossierDynamicFieldType.Time
                    => GetNullableComparableOperators(),
                DossierDynamicFieldType.Checkbox => new List<DossierComparisonOperator>
                {
                    DossierComparisonOperator.Equals,
                    DossierComparisonOperator.NotEquals,
                    DossierComparisonOperator.In,
                    DossierComparisonOperator.NotIn,
                    DossierComparisonOperator.IsNull,
                    DossierComparisonOperator.IsNotNull
                },
                _ => GetStringOperators()
            };
        }

        private static List<DossierComparisonOperator> GetStringOperators()
        {
            return new List<DossierComparisonOperator>
            {
                DossierComparisonOperator.Equals,
                DossierComparisonOperator.NotEquals,
                DossierComparisonOperator.Contains,
                DossierComparisonOperator.StartsWith,
                DossierComparisonOperator.EndsWith,
                DossierComparisonOperator.In,
                DossierComparisonOperator.NotIn,
                DossierComparisonOperator.IsNull,
                DossierComparisonOperator.IsNotNull,
                DossierComparisonOperator.IsEmpty,
                DossierComparisonOperator.IsNotEmpty
            };
        }

        private static List<DossierComparisonOperator> GetComparableOperators()
        {
            return new List<DossierComparisonOperator>
            {
                DossierComparisonOperator.Equals,
                DossierComparisonOperator.NotEquals,
                DossierComparisonOperator.GreaterThan,
                DossierComparisonOperator.GreaterThanOrEqual,
                DossierComparisonOperator.LessThan,
                DossierComparisonOperator.LessThanOrEqual,
                DossierComparisonOperator.Between,
                DossierComparisonOperator.In,
                DossierComparisonOperator.NotIn
            };
        }

        private static List<DossierComparisonOperator> GetNullableComparableOperators()
        {
            var operators = GetComparableOperators();
            operators.Add(DossierComparisonOperator.IsNull);
            operators.Add(DossierComparisonOperator.IsNotNull);
            return operators;
        }

        private static List<DossierComparisonOperator> GetEnumOperators()
        {
            return new List<DossierComparisonOperator>
            {
                DossierComparisonOperator.Equals,
                DossierComparisonOperator.NotEquals,
                DossierComparisonOperator.In,
                DossierComparisonOperator.NotIn
            };
        }

        private sealed class DossierSearchExpressionBuilder
        {
            private static readonly MethodInfoCache Methods = new();

            public Expression<Func<Dossier, bool>> Build(DossierConditionGroupDto root)
            {
                if (root == null)
                {
                    return _ => true;
                }

                return BuildGroup(root);
            }

            private Expression<Func<Dossier, bool>> BuildGroup(DossierConditionGroupDto group)
            {
                var predicates = new List<Expression<Func<Dossier, bool>>>();
                foreach (var rule in group.Rules)
                {
                    predicates.Add(BuildRule(rule));
                }

                foreach (var childGroup in group.Groups)
                {
                    predicates.Add(BuildGroup(childGroup));
                }

                if (predicates.Count == 0)
                {
                    return _ => true;
                }

                var current = predicates[0];
                for (var i = 1; i < predicates.Count; i++)
                {
                    current = group.Operator == DossierLogicalOperator.Or
                        ? OrElse(current, predicates[i])
                        : AndAlso(current, predicates[i]);
                }

                return current;
            }

            private Expression<Func<Dossier, bool>> BuildRule(DossierConditionRuleDto rule)
            {
                return rule.Source switch
                {
                    DossierSearchFieldSource.Dossier => BuildDossierRule(rule),
                    DossierSearchFieldSource.Owner => BuildOwnerRule(rule),
                    DossierSearchFieldSource.DynamicField => BuildDynamicFieldRule(rule),
                    _ => throw BuildValidationException(nameof(rule.Source), $"Unsupported source '{rule.Source}'.")
                };
            }

            private Expression<Func<Dossier, bool>> BuildDossierRule(DossierConditionRuleDto rule)
            {
                return NormalizeKey(rule.Field) switch
                {
                    "dossiernumber" => WrapDossierBody(rule, p => BuildStringBody(Expression.Property(p, nameof(Dossier.DossierNumber)), rule)),
                    "externalcasenumber" => WrapDossierBody(rule, p => BuildStringBody(Expression.Property(p, nameof(Dossier.ExternalCaseNumber)), rule)),
                    "address" => WrapDossierBody(rule, p => BuildStringBody(Expression.Property(p, nameof(Dossier.Address)), rule)),
                    "defaultimagepath" => WrapDossierBody(rule, p => BuildStringBody(Expression.Property(p, nameof(Dossier.DefaultImagePath)), rule)),
                    "renovationcode" => WrapDossierBody(rule, p => BuildStringBody(Expression.Property(p, nameof(Dossier.RenovationCode)), rule)),
                    "postalcode" => WrapDossierBody(rule, p => BuildStringBody(Expression.Property(p, nameof(Dossier.PostalCode)), rule)),
                    "registrationplate" => WrapDossierBody(rule, p => BuildStringBody(Expression.Property(p, nameof(Dossier.RegistrationPlate)), rule)),
                    "usagetype" => WrapDossierBody(rule, p => BuildEnumBody<DossierUsageType>(Expression.Property(p, nameof(Dossier.UsageType)), rule)),
                    "latitude" => WrapDossierBody(rule, p => BuildNullableComparableBody<decimal>(Expression.Property(p, nameof(Dossier.Latitude)), rule)),
                    "longitude" => WrapDossierBody(rule, p => BuildNullableComparableBody<decimal>(Expression.Property(p, nameof(Dossier.Longitude)), rule)),
                    "classificationid" => WrapDossierBody(rule, p => BuildComparableBody<long>(Expression.Property(p, nameof(Dossier.ClassificationId)), rule)),
                    _ => throw BuildValidationException(nameof(rule.Field), $"Unsupported dossier field '{rule.Field}'.")
                };
            }

            private Expression<Func<Dossier, bool>> BuildOwnerRule(DossierConditionRuleDto rule)
            {
                var dossierParam = Expression.Parameter(typeof(Dossier), "d");
                var ownerLinkParam = Expression.Parameter(typeof(DossierOwner), "o");
                var ownerEntity = Expression.Property(ownerLinkParam, nameof(DossierOwner.Owner));
                var firstName = Expression.Coalesce(
                    Expression.Property(ownerEntity, nameof(Owner.FirstName)),
                    Expression.Constant(string.Empty, typeof(string)));
                var lastName = Expression.Coalesce(
                    Expression.Property(ownerEntity, nameof(Owner.LastName)),
                    Expression.Constant(string.Empty, typeof(string)));
                var companyName = Expression.Coalesce(
                    Expression.Property(ownerEntity, nameof(Owner.CompanyName)),
                    Expression.Constant(string.Empty, typeof(string)));
                var naturalFullName = Expression.Call(
                    Methods.StringConcat3,
                    firstName,
                    Expression.Constant(" ", typeof(string)),
                    lastName);
                var ownerName = Expression.Condition(
                    Expression.Equal(
                        Expression.Property(ownerEntity, nameof(Owner.OwnerType)),
                        Expression.Constant(OwnerType.Natural)),
                    naturalFullName,
                    companyName);
                var ownerIdentifier = Expression.Condition(
                    Expression.Equal(
                        Expression.Property(ownerEntity, nameof(Owner.OwnerType)),
                        Expression.Constant(OwnerType.Natural)),
                    Expression.Property(ownerEntity, nameof(Owner.NationalCode)),
                    Expression.Property(ownerEntity, nameof(Owner.NationalId)));

                Expression ownerBody = NormalizeKey(rule.Field) switch
                {
                    "fullname" => BuildStringBody(ownerName, rule),
                    "firstname" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.FirstName)), rule),
                    "lastname" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.LastName)), rule),
                    "companyname" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.CompanyName)), rule),
                    "nationalcode" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.NationalCode)), rule),
                    "nationalid" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.NationalId)), rule),
                    "mobilenumber" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.Mobile)), rule),
                    "mobile" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.Mobile)), rule),
                    "phonenumber" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.HomePhone)), rule),
                    "homephone" => BuildStringBody(Expression.Property(ownerEntity, nameof(Owner.HomePhone)), rule),
                    "ownertype" => BuildEnumBody<OwnerType>(Expression.Property(ownerEntity, nameof(Owner.OwnerType)), rule),
                    "persontype" => BuildEnumBody<OwnerType>(Expression.Property(ownerEntity, nameof(Owner.OwnerType)), rule),
                    // Backward-compatible aliases from the previous owner model.
                    "nameortitle" => BuildStringBody(ownerName, rule),
                    "nationalidentifier" => BuildStringBody(ownerIdentifier, rule),
                    _ => throw BuildValidationException(nameof(rule.Field), $"Unsupported owner field '{rule.Field}'.")
                };

                var activeOwnerCheck = Expression.Equal(
                    Expression.Property(ownerLinkParam, nameof(DossierOwner.DeletedAt)),
                    Expression.Constant(null, typeof(DateTime?)));

                ownerBody = Expression.AndAlso(activeOwnerCheck, ownerBody);
                var ownerLambda = Expression.Lambda<Func<DossierOwner, bool>>(ownerBody, ownerLinkParam);

                var ownersProperty = Expression.Property(dossierParam, nameof(Dossier.Owners));
                var anyOwnersMatch = Expression.Call(
                    Methods.EnumerableAnyWithPredicate.MakeGenericMethod(typeof(DossierOwner)),
                    ownersProperty,
                    ownerLambda);

                return Expression.Lambda<Func<Dossier, bool>>(anyOwnersMatch, dossierParam);
            }

            private Expression<Func<Dossier, bool>> BuildDynamicFieldRule(DossierConditionRuleDto rule)
            {
                var apiName = string.IsNullOrWhiteSpace(rule.DynamicFieldApiName) ? rule.Field : rule.DynamicFieldApiName;
                if (string.IsNullOrWhiteSpace(apiName))
                {
                    throw BuildValidationException(nameof(rule.DynamicFieldApiName), "Dynamic field api name is required.");
                }

                if (!rule.DynamicFieldType.HasValue)
                {
                    throw BuildValidationException(nameof(rule.DynamicFieldType), "Dynamic field type is required for dynamic field rules.");
                }

                var dossierParam = Expression.Parameter(typeof(Dossier), "d");
                var valueParam = Expression.Parameter(typeof(DossierDynamicFieldValue), "v");

                var valueNotDeleted = Expression.Equal(
                    Expression.Property(valueParam, nameof(DossierDynamicFieldValue.DeletedAt)),
                    Expression.Constant(null, typeof(DateTime?)));

                var fieldProperty = Expression.Property(valueParam, nameof(DossierDynamicFieldValue.DynamicField));
                var fieldNotDeleted = Expression.Equal(
                    Expression.Property(fieldProperty, nameof(DossierDynamicField.DeletedAt)),
                    Expression.Constant(null, typeof(DateTime?)));
                var fieldIsActive = Expression.Equal(
                    Expression.Property(fieldProperty, nameof(DossierDynamicField.IsActive)),
                    Expression.Constant(true));
                var fieldApiNameMatches = Expression.Equal(
                    Expression.Property(fieldProperty, nameof(DossierDynamicField.ApiName)),
                    Expression.Constant(apiName.Trim(), typeof(string)));

                var typedBody = rule.DynamicFieldType.Value switch
                {
                    DossierDynamicFieldType.Text => BuildStringBody(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.TextValue)), rule),
                    DossierDynamicFieldType.MultilineText => BuildStringBody(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.TextValue)), rule),
                    DossierDynamicFieldType.Dropdown => BuildStringBody(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.OptionValue)), rule),
                    DossierDynamicFieldType.Integer => BuildNullableComparableBody<int>(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.IntegerValue)), rule),
                    DossierDynamicFieldType.Decimal => BuildNullableComparableBody<decimal>(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.DecimalValue)), rule),
                    DossierDynamicFieldType.Date => BuildNullableComparableBody<DateOnly>(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.DateValue)), rule),
                    DossierDynamicFieldType.DateTime => BuildNullableComparableBody<DateTime>(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.DateTimeValue)), rule),
                    DossierDynamicFieldType.Time => BuildNullableComparableBody<TimeSpan>(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.TimeValue)), rule),
                    DossierDynamicFieldType.Checkbox => BuildBooleanBody(Expression.Property(valueParam, nameof(DossierDynamicFieldValue.CheckboxValue)), rule, isNullable: true),
                    _ => throw BuildValidationException(nameof(rule.DynamicFieldType), $"Unsupported dynamic field type '{rule.DynamicFieldType.Value}'.")
                };

                var valueBody = Expression.AndAlso(
                    valueNotDeleted,
                    Expression.AndAlso(
                        fieldNotDeleted,
                        Expression.AndAlso(fieldIsActive, Expression.AndAlso(fieldApiNameMatches, typedBody))));

                var valueLambda = Expression.Lambda<Func<DossierDynamicFieldValue, bool>>(valueBody, valueParam);

                var valuesProperty = Expression.Property(dossierParam, nameof(Dossier.DynamicFieldValues));
                var anyValuesMatch = Expression.Call(
                    Methods.EnumerableAnyWithPredicate.MakeGenericMethod(typeof(DossierDynamicFieldValue)),
                    valuesProperty,
                    valueLambda);

                return Expression.Lambda<Func<Dossier, bool>>(anyValuesMatch, dossierParam);
            }

            private static Expression<Func<Dossier, bool>> WrapDossierBody(
                DossierConditionRuleDto rule,
                Func<ParameterExpression, Expression> bodyFactory)
            {
                var param = Expression.Parameter(typeof(Dossier), "d");
                var body = bodyFactory(param);
                return Expression.Lambda<Func<Dossier, bool>>(body, param);
            }

            private Expression BuildStringBody(Expression member, DossierConditionRuleDto rule)
            {
                return rule.Operator switch
                {
                    DossierComparisonOperator.Equals => Expression.Equal(member, Expression.Constant(ReadSingleString(rule), typeof(string))),
                    DossierComparisonOperator.NotEquals => Expression.NotEqual(member, Expression.Constant(ReadSingleString(rule), typeof(string))),
                    DossierComparisonOperator.Contains => BuildStringMethodBody(member, Methods.StringContains, rule),
                    DossierComparisonOperator.StartsWith => BuildStringMethodBody(member, Methods.StringStartsWith, rule),
                    DossierComparisonOperator.EndsWith => BuildStringMethodBody(member, Methods.StringEndsWith, rule),
                    DossierComparisonOperator.In => BuildStringSetBody(member, rule, negate: false),
                    DossierComparisonOperator.NotIn => BuildStringSetBody(member, rule, negate: true),
                    DossierComparisonOperator.IsNull => Expression.Equal(member, Expression.Constant(null, typeof(string))),
                    DossierComparisonOperator.IsNotNull => Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
                    DossierComparisonOperator.IsEmpty => Expression.OrElse(
                        Expression.Equal(member, Expression.Constant(null, typeof(string))),
                        Expression.Equal(member, Expression.Constant(string.Empty, typeof(string)))),
                    DossierComparisonOperator.IsNotEmpty => Expression.AndAlso(
                        Expression.NotEqual(member, Expression.Constant(null, typeof(string))),
                        Expression.NotEqual(member, Expression.Constant(string.Empty, typeof(string)))),
                    _ => throw BuildValidationException(nameof(rule.Operator), $"Operator '{rule.Operator}' is not valid for string fields.")
                };
            }

            private Expression BuildStringMethodBody(Expression member, MethodInfo method, DossierConditionRuleDto rule)
            {
                var value = ReadSingleString(rule);
                var memberNotNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
                var call = Expression.Call(member, method, Expression.Constant(value, typeof(string)));
                return Expression.AndAlso(memberNotNull, call);
            }

            private Expression BuildStringSetBody(Expression member, DossierConditionRuleDto rule, bool negate)
            {
                var values = ReadStringValues(rule).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (values.Length == 0)
                {
                    throw BuildValidationException(nameof(rule.Values), $"Operator '{rule.Operator}' requires at least one value.");
                }

                var containsCall = Expression.Call(
                    Methods.EnumerableContains.MakeGenericMethod(typeof(string)),
                    Expression.Constant(values),
                    member);

                return negate ? Expression.Not(containsCall) : containsCall;
            }

            private Expression BuildComparableBody<T>(Expression member, DossierConditionRuleDto rule)
                where T : struct, IComparable<T>
            {
                if (member.Type != typeof(T))
                {
                    throw new InvalidOperationException($"Expected member of type '{typeof(T)}' but received '{member.Type}'.");
                }

                return rule.Operator switch
                {
                    DossierComparisonOperator.Equals => Expression.Equal(member, Expression.Constant(ReadSingleValue<T>(rule), typeof(T))),
                    DossierComparisonOperator.NotEquals => Expression.NotEqual(member, Expression.Constant(ReadSingleValue<T>(rule), typeof(T))),
                    DossierComparisonOperator.GreaterThan => Expression.GreaterThan(member, Expression.Constant(ReadSingleValue<T>(rule), typeof(T))),
                    DossierComparisonOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, Expression.Constant(ReadSingleValue<T>(rule), typeof(T))),
                    DossierComparisonOperator.LessThan => Expression.LessThan(member, Expression.Constant(ReadSingleValue<T>(rule), typeof(T))),
                    DossierComparisonOperator.LessThanOrEqual => Expression.LessThanOrEqual(member, Expression.Constant(ReadSingleValue<T>(rule), typeof(T))),
                    DossierComparisonOperator.Between => BuildComparableBetweenBody<T>(member, rule),
                    DossierComparisonOperator.In => BuildComparableSetBody<T>(member, rule, negate: false),
                    DossierComparisonOperator.NotIn => BuildComparableSetBody<T>(member, rule, negate: true),
                    _ => throw BuildValidationException(nameof(rule.Operator), $"Operator '{rule.Operator}' is not valid for type '{typeof(T).Name}'.")
                };
            }

            private Expression BuildNullableComparableBody<T>(Expression member, DossierConditionRuleDto rule)
                where T : struct, IComparable<T>
            {
                if (member.Type != typeof(T?))
                {
                    throw new InvalidOperationException($"Expected member of type '{typeof(T?)}' but received '{member.Type}'.");
                }

                var hasValue = Expression.Property(member, nameof(Nullable<int>.HasValue));
                var value = Expression.Property(member, nameof(Nullable<int>.Value));

                return rule.Operator switch
                {
                    DossierComparisonOperator.Equals => Expression.Equal(member, Expression.Constant((T?)ReadSingleValue<T>(rule), typeof(T?))),
                    DossierComparisonOperator.NotEquals => Expression.NotEqual(member, Expression.Constant((T?)ReadSingleValue<T>(rule), typeof(T?))),
                    DossierComparisonOperator.GreaterThan => Expression.AndAlso(hasValue, Expression.GreaterThan(value, Expression.Constant(ReadSingleValue<T>(rule), typeof(T)))),
                    DossierComparisonOperator.GreaterThanOrEqual => Expression.AndAlso(hasValue, Expression.GreaterThanOrEqual(value, Expression.Constant(ReadSingleValue<T>(rule), typeof(T)))),
                    DossierComparisonOperator.LessThan => Expression.AndAlso(hasValue, Expression.LessThan(value, Expression.Constant(ReadSingleValue<T>(rule), typeof(T)))),
                    DossierComparisonOperator.LessThanOrEqual => Expression.AndAlso(hasValue, Expression.LessThanOrEqual(value, Expression.Constant(ReadSingleValue<T>(rule), typeof(T)))),
                    DossierComparisonOperator.Between => BuildNullableComparableBetweenBody<T>(hasValue, value, rule),
                    DossierComparisonOperator.In => BuildNullableComparableSetBody<T>(hasValue, value, rule, negate: false),
                    DossierComparisonOperator.NotIn => BuildNullableComparableSetBody<T>(hasValue, value, rule, negate: true),
                    DossierComparisonOperator.IsNull => Expression.Equal(member, Expression.Constant(null, typeof(T?))),
                    DossierComparisonOperator.IsNotNull => Expression.NotEqual(member, Expression.Constant(null, typeof(T?))),
                    _ => throw BuildValidationException(nameof(rule.Operator), $"Operator '{rule.Operator}' is not valid for nullable type '{typeof(T).Name}'.")
                };
            }

            private Expression BuildBooleanBody(Expression member, DossierConditionRuleDto rule, bool isNullable)
            {
                if (isNullable && member.Type != typeof(bool?))
                {
                    throw new InvalidOperationException($"Expected member of type '{typeof(bool?)}' but received '{member.Type}'.");
                }

                if (!isNullable && member.Type != typeof(bool))
                {
                    throw new InvalidOperationException($"Expected member of type '{typeof(bool)}' but received '{member.Type}'.");
                }

                if (isNullable)
                {
                    var hasValue = Expression.Property(member, nameof(Nullable<bool>.HasValue));
                    var value = Expression.Property(member, nameof(Nullable<bool>.Value));

                    return rule.Operator switch
                    {
                        DossierComparisonOperator.Equals => Expression.Equal(member, Expression.Constant((bool?)ReadSingleValue<bool>(rule), typeof(bool?))),
                        DossierComparisonOperator.NotEquals => Expression.NotEqual(member, Expression.Constant((bool?)ReadSingleValue<bool>(rule), typeof(bool?))),
                        DossierComparisonOperator.In => BuildNullableComparableSetBody<bool>(hasValue, value, rule, negate: false),
                        DossierComparisonOperator.NotIn => BuildNullableComparableSetBody<bool>(hasValue, value, rule, negate: true),
                        DossierComparisonOperator.IsNull => Expression.Equal(member, Expression.Constant(null, typeof(bool?))),
                        DossierComparisonOperator.IsNotNull => Expression.NotEqual(member, Expression.Constant(null, typeof(bool?))),
                        _ => throw BuildValidationException(nameof(rule.Operator), $"Operator '{rule.Operator}' is not valid for boolean fields.")
                    };
                }

                return rule.Operator switch
                {
                    DossierComparisonOperator.Equals => Expression.Equal(member, Expression.Constant(ReadSingleValue<bool>(rule), typeof(bool))),
                    DossierComparisonOperator.NotEquals => Expression.NotEqual(member, Expression.Constant(ReadSingleValue<bool>(rule), typeof(bool))),
                    DossierComparisonOperator.In => BuildComparableSetBody<bool>(member, rule, negate: false),
                    DossierComparisonOperator.NotIn => BuildComparableSetBody<bool>(member, rule, negate: true),
                    _ => throw BuildValidationException(nameof(rule.Operator), $"Operator '{rule.Operator}' is not valid for boolean fields.")
                };
            }

            private Expression BuildEnumBody<TEnum>(Expression member, DossierConditionRuleDto rule)
                where TEnum : struct, Enum
            {
                if (member.Type != typeof(TEnum))
                {
                    throw new InvalidOperationException($"Expected member of type '{typeof(TEnum)}' but received '{member.Type}'.");
                }

                return rule.Operator switch
                {
                    DossierComparisonOperator.Equals => Expression.Equal(member, Expression.Constant(ReadSingleEnumValue<TEnum>(rule), typeof(TEnum))),
                    DossierComparisonOperator.NotEquals => Expression.NotEqual(member, Expression.Constant(ReadSingleEnumValue<TEnum>(rule), typeof(TEnum))),
                    DossierComparisonOperator.In => BuildEnumSetBody<TEnum>(member, rule, negate: false),
                    DossierComparisonOperator.NotIn => BuildEnumSetBody<TEnum>(member, rule, negate: true),
                    _ => throw BuildValidationException(nameof(rule.Operator), $"Operator '{rule.Operator}' is not valid for enum fields.")
                };
            }

            private Expression BuildComparableBetweenBody<T>(Expression member, DossierConditionRuleDto rule)
                where T : struct, IComparable<T>
            {
                var values = ReadValues<T>(rule, 2);
                return Expression.AndAlso(
                    Expression.GreaterThanOrEqual(member, Expression.Constant(values[0], typeof(T))),
                    Expression.LessThanOrEqual(member, Expression.Constant(values[1], typeof(T))));
            }

            private Expression BuildNullableComparableBetweenBody<T>(Expression hasValue, Expression value, DossierConditionRuleDto rule)
                where T : struct, IComparable<T>
            {
                var values = ReadValues<T>(rule, 2);
                var lower = Expression.GreaterThanOrEqual(value, Expression.Constant(values[0], typeof(T)));
                var upper = Expression.LessThanOrEqual(value, Expression.Constant(values[1], typeof(T)));
                return Expression.AndAlso(hasValue, Expression.AndAlso(lower, upper));
            }

            private Expression BuildComparableSetBody<T>(Expression member, DossierConditionRuleDto rule, bool negate)
                where T : struct, IComparable<T>
            {
                var values = ReadValues<T>(rule, minimumCount: 1);
                var containsCall = Expression.Call(
                    Methods.EnumerableContains.MakeGenericMethod(typeof(T)),
                    Expression.Constant(values),
                    member);
                return negate ? Expression.Not(containsCall) : containsCall;
            }

            private Expression BuildNullableComparableSetBody<T>(Expression hasValue, Expression value, DossierConditionRuleDto rule, bool negate)
                where T : struct, IComparable<T>
            {
                var values = ReadValues<T>(rule, minimumCount: 1);
                var containsCall = Expression.Call(
                    Methods.EnumerableContains.MakeGenericMethod(typeof(T)),
                    Expression.Constant(values),
                    value);
                var body = Expression.AndAlso(hasValue, containsCall);
                return negate ? Expression.Not(body) : body;
            }

            private Expression BuildEnumSetBody<TEnum>(Expression member, DossierConditionRuleDto rule, bool negate)
                where TEnum : struct, Enum
            {
                var values = ReadEnumValues<TEnum>(rule, minimumCount: 1);
                var containsCall = Expression.Call(
                    Methods.EnumerableContains.MakeGenericMethod(typeof(TEnum)),
                    Expression.Constant(values),
                    member);
                return negate ? Expression.Not(containsCall) : containsCall;
            }

            private static T ReadSingleValue<T>(DossierConditionRuleDto rule) where T : struct
            {
                var values = ReadValues<T>(rule, exactCount: 1);
                return values[0];
            }

            private static T[] ReadValues<T>(DossierConditionRuleDto rule, int? exactCount = null, int? minimumCount = null)
                where T : struct
            {
                var elements = ReadElements(rule);
                if (exactCount.HasValue && elements.Count != exactCount.Value)
                {
                    throw BuildValidationException(nameof(rule.Values), $"Operator '{rule.Operator}' requires exactly {exactCount.Value} value(s).");
                }

                if (minimumCount.HasValue && elements.Count < minimumCount.Value)
                {
                    throw BuildValidationException(nameof(rule.Values), $"Operator '{rule.Operator}' requires at least {minimumCount.Value} value(s).");
                }

                return elements.Select(ReadTypedValue<T>).ToArray();
            }

            private static string ReadSingleString(DossierConditionRuleDto rule)
            {
                var elements = ReadElements(rule);
                if (elements.Count != 1)
                {
                    throw BuildValidationException(nameof(rule.Value), $"Operator '{rule.Operator}' requires exactly one value.");
                }

                return ReadString(elements[0]);
            }

            private static string[] ReadStringValues(DossierConditionRuleDto rule)
            {
                var elements = ReadElements(rule);
                return elements.Select(ReadString).ToArray();
            }

            private static TEnum ReadSingleEnumValue<TEnum>(DossierConditionRuleDto rule)
                where TEnum : struct, Enum
            {
                var values = ReadEnumValues<TEnum>(rule, exactCount: 1);
                return values[0];
            }

            private static TEnum[] ReadEnumValues<TEnum>(DossierConditionRuleDto rule, int? exactCount = null, int? minimumCount = null)
                where TEnum : struct, Enum
            {
                var elements = ReadElements(rule);
                if (exactCount.HasValue && elements.Count != exactCount.Value)
                {
                    throw BuildValidationException(nameof(rule.Values), $"Operator '{rule.Operator}' requires exactly {exactCount.Value} value(s).");
                }

                if (minimumCount.HasValue && elements.Count < minimumCount.Value)
                {
                    throw BuildValidationException(nameof(rule.Values), $"Operator '{rule.Operator}' requires at least {minimumCount.Value} value(s).");
                }

                return elements.Select(ReadEnumValue<TEnum>).ToArray();
            }

            private static List<JsonElement> ReadElements(DossierConditionRuleDto rule)
            {
                if (rule.Values.Count > 0)
                {
                    return rule.Values;
                }

                if (!rule.Value.HasValue || rule.Value.Value.ValueKind == JsonValueKind.Null || rule.Value.Value.ValueKind == JsonValueKind.Undefined)
                {
                    return new List<JsonElement>();
                }

                if (rule.Value.Value.ValueKind == JsonValueKind.Array)
                {
                    return rule.Value.Value.EnumerateArray().ToList();
                }

                return new List<JsonElement> { rule.Value.Value };
            }

            private static string ReadString(JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    return element.GetString()?.Trim() ?? string.Empty;
                }

                if (element.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                {
                    return element.ToString().Trim();
                }

                throw BuildValidationException(nameof(DossierConditionRuleDto.Value), "Value must be a string.");
            }

            private static T ReadTypedValue<T>(JsonElement element) where T : struct
            {
                var targetType = typeof(T);
                try
                {
                    if (targetType == typeof(int))
                    {
                        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var i))
                        {
                            return (T)(object)i;
                        }

                        if (int.TryParse(ReadString(element), NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
                        {
                            return (T)(object)i;
                        }
                    }
                    else if (targetType == typeof(long))
                    {
                        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var l))
                        {
                            return (T)(object)l;
                        }

                        if (long.TryParse(ReadString(element), NumberStyles.Integer, CultureInfo.InvariantCulture, out l))
                        {
                            return (T)(object)l;
                        }
                    }
                    else if (targetType == typeof(decimal))
                    {
                        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var d))
                        {
                            return (T)(object)d;
                        }

                        if (decimal.TryParse(ReadString(element), NumberStyles.Number, CultureInfo.InvariantCulture, out d))
                        {
                            return (T)(object)d;
                        }
                    }
                    else if (targetType == typeof(bool))
                    {
                        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        {
                            return (T)(object)element.GetBoolean();
                        }

                        if (bool.TryParse(ReadString(element), out var b))
                        {
                            return (T)(object)b;
                        }
                    }
                    else if (targetType == typeof(DateTime))
                    {
                        if (element.ValueKind == JsonValueKind.String &&
                            DateTime.TryParse(
                                element.GetString(),
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                out var dateTime))
                        {
                            return (T)(object)dateTime;
                        }
                    }
                    else if (targetType == typeof(DateOnly))
                    {
                        if (element.ValueKind == JsonValueKind.String &&
                            DateOnly.TryParse(
                                element.GetString(),
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out var date))
                        {
                            return (T)(object)date;
                        }
                    }
                    else if (targetType == typeof(TimeSpan))
                    {
                        if (element.ValueKind == JsonValueKind.String &&
                            TimeSpan.TryParse(element.GetString(), CultureInfo.InvariantCulture, out var time))
                        {
                            return (T)(object)time;
                        }
                    }
                }
                catch (FormatException)
                {
                    // The unified validation message below is sufficient for API consumers.
                }

                throw BuildValidationException(nameof(DossierConditionRuleDto.Value), $"Value '{element}' is not valid for type '{targetType.Name}'.");
            }

            private static TEnum ReadEnumValue<TEnum>(JsonElement element)
                where TEnum : struct, Enum
            {
                if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var intValue))
                {
                    var parsedByNumber = (TEnum)Enum.ToObject(typeof(TEnum), intValue);
                    if (Enum.IsDefined(parsedByNumber))
                    {
                        return parsedByNumber;
                    }
                }

                if (element.ValueKind == JsonValueKind.String)
                {
                    var raw = element.GetString();
                    if (!string.IsNullOrWhiteSpace(raw) && Enum.TryParse<TEnum>(raw, true, out var parsedByText))
                    {
                        return parsedByText;
                    }
                }

                throw BuildValidationException(nameof(DossierConditionRuleDto.Value), $"Value '{element}' is not valid for enum '{typeof(TEnum).Name}'.");
            }

            private static string NormalizeKey(string key)
            {
                return new string(key
                    .Where(char.IsLetterOrDigit)
                    .Select(char.ToLowerInvariant)
                    .ToArray());
            }

            private static ValidationException BuildValidationException(string propertyName, string message)
            {
                return new ValidationException(new[]
                {
                    new ValidationFailure(propertyName, message)
                });
            }

            private static Expression<Func<T, bool>> AndAlso<T>(
                Expression<Func<T, bool>> left,
                Expression<Func<T, bool>> right)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
                var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
            }

            private static Expression<Func<T, bool>> OrElse<T>(
                Expression<Func<T, bool>> left,
                Expression<Func<T, bool>> right)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var leftBody = ReplaceParameter(left.Body, left.Parameters[0], parameter);
                var rightBody = ReplaceParameter(right.Body, right.Parameters[0], parameter);
                return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), parameter);
            }

            private static Expression ReplaceParameter(Expression expression, ParameterExpression source, ParameterExpression target)
            {
                return new ParameterReplaceVisitor(source, target).Visit(expression)!;
            }

            private sealed class ParameterReplaceVisitor : ExpressionVisitor
            {
                private readonly ParameterExpression _source;
                private readonly ParameterExpression _target;

                public ParameterReplaceVisitor(ParameterExpression source, ParameterExpression target)
                {
                    _source = source;
                    _target = target;
                }

                protected override Expression VisitParameter(ParameterExpression node)
                {
                    return node == _source ? _target : base.VisitParameter(node);
                }
            }

            private sealed class MethodInfoCache
            {
                public MethodInfo StringConcat3 { get; } = typeof(string)
                    .GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string), typeof(string) })!;
                public MethodInfo StringContains { get; } = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
                public MethodInfo StringStartsWith { get; } = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
                public MethodInfo StringEndsWith { get; } = typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!;
                public MethodInfo EnumerableContains { get; } = typeof(Enumerable)
                    .GetMethods()
                    .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);
                public MethodInfo EnumerableAnyWithPredicate { get; } = typeof(Enumerable)
                    .GetMethods()
                    .Single(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2);
            }
        }
    }
}
