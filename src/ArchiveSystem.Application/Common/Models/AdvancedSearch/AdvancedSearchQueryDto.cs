namespace ArchiveSystem.Application.Common.Models.AdvancedSearch
{
    public enum AdvancedSearchSortBy
    {
        Relevance = 0,
        Date = 1,
        FileName = 2,
    }

    public enum SortDirection
    {
        Desc = 0,
        Asc = 1,
    }

    public enum TagSearchMode
    {
        Any = 0,
        All = 1,
    }

    public sealed class AdvancedSearchQueryDto
    {
        public string? GeneralSearchTerm { get; set; }
        public string? DossierNumber { get; set; }
        public string? PostalCode { get; set; }
        public string? OwnerFirstName { get; set; }
        public string? OwnerLastName { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? CreatedBy { get; set; }
        public List<string>? Tags { get; set; }
        public TagSearchMode TagMode { get; set; } = TagSearchMode.Any;
        public AdvancedSearchSortBy SortBy { get; set; } = AdvancedSearchSortBy.Relevance;
        public SortDirection SortDirection { get; set; } = SortDirection.Desc;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
