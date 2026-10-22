using System.Security.Claims;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Features.Taxpayers.Commands.CreateTaxpayer;
using ArchiveSystem.Application.Features.Taxpayers.Commands.DeleteTaxpayer;
using ArchiveSystem.Application.Features.Taxpayers.Commands.UpdateTaxpayer;
using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.Taxpayers.Queries.GetPagedTaxpayers;
using ArchiveSystem.Application.Features.Taxpayers.Queries.GetTaxpayerById;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Presentation.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArchiveSystem.Presentation.Api.Authorization;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [RequirePermission(Permissions.Taxpayers.Manage)]
    [ApiController]
    [Route("api/v1/taxpayers")]
    public sealed class TaxpayersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TaxpayersController> _logger;

        public TaxpayersController(IMediator mediator, ILogger<TaxpayersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<PagedResponse<TaxpayerListItemDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<PagedResponse<TaxpayerListItemDto>>>> GetTaxpayers(
            [FromQuery] GetPagedTaxpayersQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResult<PagedResponse<TaxpayerListItemDto>>.Ok(result));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResult<TaxpayerDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResult<TaxpayerDetailsDto>>> GetTaxpayerById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTaxpayerByIdQuery(id), cancellationToken);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(ApiResult<TaxpayerDetailsDto>.Ok(result));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResult<TaxpayerDetailsDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResult<TaxpayerDetailsDto>>> CreateTaxpayer(
            [FromBody] CreateTaxpayerCommand command,
            CancellationToken cancellationToken)
        {
            var created = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Admin {AdminId} created taxpayer {TaxpayerId}", GetActorId(), created.Id);

            return CreatedAtAction(
                nameof(GetTaxpayerById),
                new { id = created.Id },
                ApiResult<TaxpayerDetailsDto>.Ok(created, "Taxpayer created successfully."));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(ApiResult<TaxpayerDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResult<TaxpayerDetailsDto>>> UpdateTaxpayer(
            Guid id,
            [FromBody] UpdateTaxpayerCommand command,
            CancellationToken cancellationToken)
        {
            var updated = await _mediator.Send(command with { Id = id }, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} updated taxpayer {TaxpayerId}", GetActorId(), id);
            return Ok(ApiResult<TaxpayerDetailsDto>.Ok(updated, "Taxpayer updated successfully."));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTaxpayer(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _mediator.Send(new DeleteTaxpayerCommand(id), cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} deleted taxpayer {TaxpayerId}", GetActorId(), id);
            return NoContent();
        }

        private string GetActorId()
        {
            return User.FindFirstValue("sub")
                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? "unknown";
        }
    }
}
