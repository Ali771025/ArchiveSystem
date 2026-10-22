// src/ArchiveSystem.Application/Features/Files/Queries/SearchFiles/SearchFilesQuery.cs
using MediatR;
using ArchiveSystem.Application.Common.Models;

namespace ArchiveSystem.Application.Features.Files.Queries.SearchFiles
{
    public class SearchFilesQuery : IRequest<PaginatedResult<FileDto>>
    {
        public string? Term { get; set; }
        public long? CaseId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "CreatedAt";
        public bool SortAscending { get; set; } = false;
        public IReadOnlyCollection<long> AllowedClassificationIds { get; set; } = Array.Empty<long>();
        public IReadOnlyCollection<long> AllowedTagIds { get; set; } = Array.Empty<long>();
        public bool BypassAuthorizationFilters { get; set; }
    }
}
