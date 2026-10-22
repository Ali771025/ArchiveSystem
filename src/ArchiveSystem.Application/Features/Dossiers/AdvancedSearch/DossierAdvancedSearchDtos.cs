using System.Text.Json;
using System.Text.Json.Serialization;
using ArchiveSystem.Domain.Enums;

namespace ArchiveSystem.Application.Features.Dossiers.AdvancedSearch
{
    public sealed class DossierAdvancedSearchRequestDto
    {
        public DossierConditionGroupDto Root { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "modifiedAt";
        public string SortDirection { get; set; } = "desc";
        public bool IncludeDeleted { get; set; }
    }

    public sealed class DossierAdvancedSearchResultDto
    {
        public List<DossierAdvancedSearchItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public sealed class DossierAdvancedSearchFieldCatalogDto
    {
        public List<DossierSearchFieldDescriptorDto> Fields { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class DossierSearchFieldDescriptorDto
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DossierSearchFieldSource Source { get; set; }
        public string Field { get; set; } = string.Empty;
        public DossierSearchValueType ValueType { get; set; }
        public bool IsNullable { get; set; }
        public List<DossierComparisonOperator> SupportedOperators { get; set; } = new();
        public string? DynamicFieldApiName { get; set; }
        public DossierDynamicFieldType? DynamicFieldType { get; set; }
        public Guid? DynamicFieldId { get; set; }
        public long? ClassificationId { get; set; }
        public List<DossierSearchFieldOptionDto> Options { get; set; } = new();
    }

    public sealed class DossierSearchFieldOptionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public sealed class DossierAdvancedSearchItemDto
    {
        public long Id { get; set; }
        public string DossierNumber { get; set; } = string.Empty;
        public string ExternalCaseNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DossierUsageType UsageType { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public long ClassificationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsDeleted { get; set; }
        public List<string> OwnerNames { get; set; } = new();
    }

    public sealed class DossierConditionGroupDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DossierLogicalOperator Operator { get; set; } = DossierLogicalOperator.And;
        public List<DossierConditionGroupDto> Groups { get; set; } = new();
        public List<DossierConditionRuleDto> Rules { get; set; } = new();
    }

    public sealed class DossierConditionRuleDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DossierSearchFieldSource Source { get; set; }
        public string Field { get; set; } = string.Empty;
        public DossierComparisonOperator Operator { get; set; }
        public JsonElement? Value { get; set; }
        public List<JsonElement> Values { get; set; } = new();
        public DossierSearchValueType? ValueType { get; set; }
        public string? DynamicFieldApiName { get; set; }
        public DossierDynamicFieldType? DynamicFieldType { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DossierLogicalOperator
    {
        And = 1,
        Or = 2
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DossierSearchFieldSource
    {
        Dossier = 1,
        Owner = 2,
        DynamicField = 3
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DossierSearchValueType
    {
        Text = 1,
        Integer = 2,
        Decimal = 3,
        Date = 4,
        DateTime = 5,
        Time = 6,
        Boolean = 7
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DossierComparisonOperator
    {
        Equals = 1,
        NotEquals = 2,
        Contains = 3,
        StartsWith = 4,
        EndsWith = 5,
        GreaterThan = 6,
        GreaterThanOrEqual = 7,
        LessThan = 8,
        LessThanOrEqual = 9,
        Between = 10,
        In = 11,
        NotIn = 12,
        IsNull = 13,
        IsNotNull = 14,
        IsEmpty = 15,
        IsNotEmpty = 16
    }
}
