using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Infrastructure.Services;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/integrations")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IBehsazIntegrationService _behsazIntegrationService;

        public IntegrationsController(IBehsazIntegrationService behsazIntegrationService)
        {
            _behsazIntegrationService = behsazIntegrationService;
        }

        [HttpGet("behsaz/config")]
        [ProducesResponseType(typeof(BehsazConfigResponse), StatusCodes.Status200OK)]
        public IActionResult GetBehsazConfig()
        {
            return Ok(new BehsazConfigResponse
            {
                Enabled = _behsazIntegrationService.IsEnabled
            });
        }

        [HttpGet("behsaz/search")]
        [ProducesResponseType(typeof(BehsazSearchResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchBehsaz(
            [FromQuery] string text,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken)
        {
            if (!_behsazIntegrationService.IsEnabled)
            {
                return Ok(new BehsazSearchResponse
                {
                    Enabled = false,
                    Items = new List<BehsazSearchItemResponse>()
                });
            }

            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure("text", "Search text must be at least 2 characters.")
                });
            }

            var items = await _behsazIntegrationService.SearchCasesAsync(new BehsazSearchQuery
            {
                Text = text,
                FromDate = fromDate,
                ToDate = toDate
            }, cancellationToken);

            return Ok(new BehsazSearchResponse
            {
                Enabled = true,
                Items = items.Select(x => new BehsazSearchItemResponse
                {
                    ExternalCaseNumber = x.ExternalCaseNumber,
                    Title = x.Title,
                    OwnerName = x.OwnerName,
                    NationalCode = x.NationalCode,
                    RegisteredAt = x.RegisteredAt
                }).ToList()
            });
        }
    }

    public sealed class BehsazConfigResponse
    {
        public bool Enabled { get; set; }
    }

    public sealed class BehsazSearchResponse
    {
        public bool Enabled { get; set; }
        public List<BehsazSearchItemResponse> Items { get; set; } = new();
    }

    public sealed class BehsazSearchItemResponse
    {
        public string ExternalCaseNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string NationalCode { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; }
    }
}
