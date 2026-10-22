// src/ArchiveSystem.Presentation.Api/Controllers/AdvancedFilesController.cs
using Microsoft.AspNetCore.Mvc;
using MediatR;
using ArchiveSystem.Domain.Services;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Common.Models.AdvancedSearch;
using ArchiveSystem.Application.Features.Files.Commands.UploadFile;
using ArchiveSystem.Application.Features.Files.Queries.SearchFiles;
using ArchiveSystem.Application.Features.UserManagement.Queries.GetUsersWithPagination;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Interfaces;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Presentation.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using FluentValidation;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/[controller]")]
    public class AdvancedFilesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IStorageService _storageService;
        private readonly IAccessControlService _accessControlService;
        private readonly IAdvancedFileSearchService _advancedFileSearchService;
        private readonly IValidator<AdvancedSearchQueryDto> _advancedSearchValidator;
        private readonly ILogger<AdvancedFilesController> _logger;

        public AdvancedFilesController(
            IMediator mediator, 
            IStorageService storageService,
            IAccessControlService accessControlService,
            IAdvancedFileSearchService advancedFileSearchService,
            IValidator<AdvancedSearchQueryDto> advancedSearchValidator,
            ILogger<AdvancedFilesController> logger)
        {
            _mediator = mediator;
            _storageService = storageService;
            _accessControlService = accessControlService;
            _advancedFileSearchService = advancedFileSearchService;
            _advancedSearchValidator = advancedSearchValidator;
            _logger = logger;
        }

        /// <summary>
        /// Upload large files with multipart support
        /// </summary>
        [HttpPost("multipart")]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
        public async Task<ActionResult<UploadFileResponse>> UploadMultipartFile(IFormFile file)
        {
            if (!await _accessControlService.HasGlobalPermissionAsync(GetUserId(), "admin.access"))
            {
                return Forbid();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required");
            }

            // Validate file type
            var allowedTypes = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedTypes.Contains(extension))
            {
                return BadRequest($"File type {extension} is not allowed");
            }

            // Generate Tus-compatible ID
            var tusId = Guid.NewGuid().ToString();
            
            // Create upload command
            var command = new UploadFileCommand
            {
                FileName = file.FileName,
                Size = file.Length,
                ContentType = file.ContentType,
                TusId = tusId,
                OriginalFileName = file.FileName
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Batch operations for multiple files
        /// </summary>
        [HttpPost("batch")]
        public async Task<ActionResult<IEnumerable<UploadFileResponse>>> BatchUpload([FromBody] IEnumerable<BatchUploadRequest> requests)
        {
            if (!await _accessControlService.HasGlobalPermissionAsync(GetUserId(), "admin.access"))
            {
                return Forbid();
            }

            var results = new List<UploadFileResponse>();
            
            foreach (var request in requests)
            {
                var command = new UploadFileCommand
                {
                    FileName = request.FileName,
                    Size = request.Size,
                    ContentType = request.ContentType,
                    TusId = Guid.NewGuid().ToString(),
                    OriginalFileName = request.FileName
                };

                var result = await _mediator.Send(command);
                results.Add(result);
            }

            return Ok(results);
        }

        /// <summary>
        /// Advanced search with faceted results
        /// </summary>
        [HttpGet("advanced-search")]
        public async Task<ActionResult<AdvancedSearchResult>> AdvancedSearch([FromQuery] AdvancedSearchQuery query)
        {
            var userId = GetUserId();
            // This would integrate with OpenSearch aggregations for faceted search
            var searchQuery = new SearchFilesQuery
            {
                Term = query.Term,
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                Page = query.Page,
                PageSize = query.PageSize,
                AllowedClassificationIds = await _accessControlService.GetAccessibleClassificationIdsAsync(userId, ClassificationPermissionMask.Read),
                AllowedTagIds = await _accessControlService.GetAllowedTagIdsAsync(userId, TagPermissionMask.View)
            };

            var results = await _mediator.Send(searchQuery);
            
            // Add facet information from OpenSearch aggregations
            var advancedResult = new AdvancedSearchResult
            {
                Results = results,
                Facets = await GetFacetsAsync(query.Term),
                Suggestions = await GetSuggestionsAsync(query.Term)
            };

            return Ok(advancedResult);
        }

        [HttpGet("/api/v1/files/advanced-search")]
        [Obsolete("Deprecated. Use POST /api/v1/search/documents instead.")]
        [ProducesResponseType(typeof(PagedSearchResultDto<SearchResultItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchFiles([FromQuery] AdvancedSearchQueryDto query, CancellationToken cancellationToken)
        {
            var validationResult = await _advancedSearchValidator.ValidateAsync(query, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    status = StatusCodes.Status400BadRequest,
                    title = "Validation failed.",
                    errors = validationResult.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => error.ErrorMessage).Distinct().ToArray())
                });
            }

            var result = await _advancedFileSearchService.AdvancedSearchAsync(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("/api/v1/files/advanced-search/users")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(typeof(IReadOnlyCollection<AdvancedSearchUserOption>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string? search,
            [FromQuery] int take = 30,
            CancellationToken cancellationToken = default)
        {
            var normalizedTake = Math.Clamp(take, 1, 50);
            var normalizedSearch = search?.Trim();

            var usernameMatches = await _mediator.Send(
                new GetUsersWithPaginationQuery
                {
                    PageNumber = 1,
                    PageSize = normalizedTake,
                    SortBy = "UserName",
                    SortDescending = false,
                    Username = normalizedSearch
                },
                cancellationToken);

            var users = usernameMatches.Items;
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var nameMatches = await _mediator.Send(
                    new GetUsersWithPaginationQuery
                    {
                        PageNumber = 1,
                        PageSize = normalizedTake,
                        SortBy = "UserName",
                        SortDescending = false,
                        Name = normalizedSearch
                    },
                    cancellationToken);

                users = users
                    .Concat(nameMatches.Items)
                    .GroupBy(user => user.Id)
                    .Select(group => group.First())
                    .Take(normalizedTake)
                    .ToList();
            }

            return Ok(users.Select(user => new AdvancedSearchUserOption(
                user.Id,
                user.Username,
                $"{user.FirstName} {user.LastName}".Trim())));
        }

        private async Task<Dictionary<string, int>> GetFacetsAsync(string? term)
        {
            // Implementation would call OpenSearch aggregations
            return new Dictionary<string, int>
            {
                ["pdf"] = 120,
                ["docx"] = 85,
                ["images"] = 45,
                ["recent"] = 67
            };
        }

        private async Task<List<string>> GetSuggestionsAsync(string? term)
        {
            // Implementation would use OpenSearch suggest API
            return new List<string>();
        }

        private long GetUserId()
        {
            var raw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var userId) ? userId : 0;
        }
    }

    public class BatchUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
    }

    public class AdvancedSearchQuery
    {
        public string? Term { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdvancedSearchResult
    {
        public PaginatedResult<FileDto> Results { get; set; } = new();
        public Dictionary<string, int> Facets { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }

    public sealed record AdvancedSearchUserOption(
        long Id,
        string Username,
        string FullName);
}
