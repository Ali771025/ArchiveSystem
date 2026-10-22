using System.Text.Json;
using FluentValidation;

namespace ArchiveSystem.Application.Features.Dossiers.AdvancedSearch
{
    public sealed class SearchDossiersAdvancedQueryValidator : AbstractValidator<SearchDossiersAdvancedQuery>
    {
        private static readonly HashSet<string> SupportedDossierFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "DossierNumber",
            "ExternalCaseNumber",
            "Address",
            "DefaultImagePath",
            "RenovationCode",
            "PostalCode",
            "RegistrationPlate",
            "UsageType",
            "Latitude",
            "Longitude",
            "ClassificationId"
        };

        private static readonly HashSet<string> SupportedOwnerFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "FullName",
            "NationalCode",
            "NationalId",
            "OwnerType",
            "PersonType",
            "Mobile",
            "MobileNumber",
            "HomePhone",
            "PhoneNumber",
            "CompanyName",
            "NameOrTitle",
            "NationalIdentifier",
            // Backward-compatible aliases from the previous owner model.
            "FirstName",
            "LastName",
        };

        public SearchDossiersAdvancedQueryValidator()
        {
            RuleFor(x => x.Request.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Request.PageSize).InclusiveBetween(1, 200);
            RuleFor(x => x.Request.SortBy)
                .MaximumLength(64)
                .Must(sort => string.IsNullOrWhiteSpace(sort) || IsSupportedSort(sort))
                .WithMessage("Unsupported sort field.");
            RuleFor(x => x.Request.SortDirection)
                .Must(direction => string.IsNullOrWhiteSpace(direction) ||
                                   direction.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                                   direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
                .WithMessage("SortDirection must be 'asc' or 'desc'.");
            RuleFor(x => x.Request.Root).NotNull();

            RuleFor(x => x).Custom((query, context) =>
            {
                if (query.Request.Root == null)
                {
                    return;
                }

                ValidateGroup(query.Request.Root, context, "Request.Root", depth: 1);
            });
        }

        private static void ValidateGroup(
            DossierConditionGroupDto group,
            ValidationContext<SearchDossiersAdvancedQuery> context,
            string path,
            int depth)
        {
            if (depth > 8)
            {
                context.AddFailure(path, "Nested groups deeper than 8 levels are not supported.");
                return;
            }

            if (group.Rules.Count == 0 && group.Groups.Count == 0)
            {
                context.AddFailure(path, "Condition group must contain at least one rule or subgroup.");
                return;
            }

            if (group.Rules.Count > 100)
            {
                context.AddFailure(path, "A single group cannot contain more than 100 rules.");
            }

            for (var i = 0; i < group.Rules.Count; i++)
            {
                ValidateRule(group.Rules[i], context, $"{path}.Rules[{i}]");
            }

            for (var i = 0; i < group.Groups.Count; i++)
            {
                ValidateGroup(group.Groups[i], context, $"{path}.Groups[{i}]", depth + 1);
            }
        }

        private static void ValidateRule(
            DossierConditionRuleDto rule,
            ValidationContext<SearchDossiersAdvancedQuery> context,
            string path)
        {
            if (string.IsNullOrWhiteSpace(rule.Field))
            {
                context.AddFailure($"{path}.Field", "Field is required.");
            }

            switch (rule.Source)
            {
                case DossierSearchFieldSource.Dossier:
                    if (!SupportedDossierFields.Contains(rule.Field))
                    {
                        context.AddFailure($"{path}.Field", $"Unsupported dossier field '{rule.Field}'.");
                    }
                    break;
                case DossierSearchFieldSource.Owner:
                    if (!SupportedOwnerFields.Contains(rule.Field))
                    {
                        context.AddFailure($"{path}.Field", $"Unsupported owner field '{rule.Field}'.");
                    }
                    break;
                case DossierSearchFieldSource.DynamicField:
                    if (!rule.DynamicFieldType.HasValue)
                    {
                        context.AddFailure($"{path}.DynamicFieldType", "DynamicFieldType is required for dynamic-field rules.");
                    }

                    if (string.IsNullOrWhiteSpace(rule.DynamicFieldApiName) && string.IsNullOrWhiteSpace(rule.Field))
                    {
                        context.AddFailure($"{path}.DynamicFieldApiName", "DynamicFieldApiName is required for dynamic-field rules.");
                    }
                    break;
                default:
                    context.AddFailure($"{path}.Source", $"Unsupported source '{rule.Source}'.");
                    break;
            }

            ValidateRuleValueShape(rule, context, path);
        }

        private static void ValidateRuleValueShape(
            DossierConditionRuleDto rule,
            ValidationContext<SearchDossiersAdvancedQuery> context,
            string path)
        {
            var count = CountValues(rule);

            switch (rule.Operator)
            {
                case DossierComparisonOperator.IsNull:
                case DossierComparisonOperator.IsNotNull:
                case DossierComparisonOperator.IsEmpty:
                case DossierComparisonOperator.IsNotEmpty:
                    if (count > 0)
                    {
                        context.AddFailure($"{path}.Value", $"Operator '{rule.Operator}' does not accept a value.");
                    }
                    return;
                case DossierComparisonOperator.Between:
                    if (count != 2)
                    {
                        context.AddFailure($"{path}.Value", "Between operator requires exactly two values.");
                    }
                    return;
                case DossierComparisonOperator.In:
                case DossierComparisonOperator.NotIn:
                    if (count < 1)
                    {
                        context.AddFailure($"{path}.Values", $"{rule.Operator} operator requires at least one value.");
                    }
                    return;
                default:
                    if (count != 1)
                    {
                        context.AddFailure($"{path}.Value", $"Operator '{rule.Operator}' requires exactly one value.");
                    }
                    return;
            }
        }

        private static int CountValues(DossierConditionRuleDto rule)
        {
            if (rule.Values.Count > 0)
            {
                return rule.Values.Count;
            }

            if (!rule.Value.HasValue)
            {
                return 0;
            }

            var value = rule.Value.Value;
            if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                return value.GetArrayLength();
            }

            return 1;
        }

        private static bool IsSupportedSort(string sortBy)
        {
            var normalized = sortBy.Trim().ToLowerInvariant();
            return normalized is "modifiedat"
                or "createdat"
                or "dossiernumber"
                or "externalcasenumber"
                or "address"
                or "usagetype";
        }
    }
}
