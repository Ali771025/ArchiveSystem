using System.Text.Json;

namespace ArchiveSystem.Presentation.Api.Models.Cases
{
    public sealed class SearchCasesRequest
    {
        public long? ClassificationId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "updatedAt";
        public string SortDirection { get; set; } = "desc";
        public string? OwnerName { get; set; }
        public string? ExternalCaseNumber { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<long> Tags { get; set; } = new();
        public List<DynamicFilterRequest> DynamicFilters { get; set; } = new();
        public DynamicFilterExpressionRequest? DynamicFilterExpression { get; set; }
    }

    public sealed class DynamicFilterExpressionRequest
    {
        public string Logic { get; set; } = "and";
        public List<DynamicFilterGroupRequest> Groups { get; set; } = new();
    }

    public sealed class DynamicFilterGroupRequest
    {
        public string Logic { get; set; } = "and";
        public List<DynamicFilterRequest> Filters { get; set; } = new();
    }

    public sealed class DynamicFilterRequest
    {
        public Guid FieldId { get; set; }
        public string Op { get; set; } = string.Empty;
        public JsonElement? Value { get; set; }
        public JsonElement? FromValue { get; set; }
        public JsonElement? ToValue { get; set; }
        public List<JsonElement>? Values { get; set; }
    }
}
