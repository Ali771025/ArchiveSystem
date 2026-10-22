namespace ArchiveSystem.Application.Common.Models.AdvancedSearch
{
    public sealed class PagedSearchResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public long TotalHits { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalHits / PageSize);
        public AdvancedSearchAggregationsDto Aggregations { get; set; } = new();
    }

    public sealed class SearchResultItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string DossierNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public List<string> Highlights { get; set; } = new();
    }

    public sealed class AdvancedSearchAggregationsDto
    {
        public Dictionary<string, long> Tags { get; set; } = new();
        public Dictionary<string, long> Dossiers { get; set; } = new();
    }
}
