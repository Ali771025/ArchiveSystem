namespace ArchiveSystem.Application.Features.UserManagement.Common
{
    public sealed class PagedResponse<T>
    {
        public required IReadOnlyList<T> Items { get; init; }
        public required int PageNumber { get; init; }
        public required int PageSize { get; init; }
        public required int TotalCount { get; init; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(PageSize, 1));
    }
}
