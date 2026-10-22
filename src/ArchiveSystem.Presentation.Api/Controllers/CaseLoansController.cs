using System.Security.Claims;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Features.CaseLoans.Commands.CreateCaseLoan;
using ArchiveSystem.Application.Features.CaseLoans.Commands.DeleteCaseLoan;
using ArchiveSystem.Application.Features.CaseLoans.Commands.UpdateCaseLoan;
using ArchiveSystem.Application.Features.CaseLoans.DTOs;
using ArchiveSystem.Application.Features.CaseLoans.Queries.GetCaseLoanById;
using ArchiveSystem.Application.Features.CaseLoans.Queries.GetPagedCaseLoans;
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
    [RequirePermission(Permissions.CaseLoans.Manage)]
    [ApiController]
    [Route("api/v1/case-loans")]
    public sealed class CaseLoansController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CaseLoansController> _logger;

        public CaseLoansController(IMediator mediator, ILogger<CaseLoansController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<PagedResponse<CaseLoanListItemDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<PagedResponse<CaseLoanListItemDto>>>> GetCaseLoans(
            [FromQuery] GetPagedCaseLoansQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResult<PagedResponse<CaseLoanListItemDto>>.Ok(result));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResult<CaseLoanDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResult<CaseLoanDetailsDto>>> GetCaseLoanById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCaseLoanByIdQuery(id), cancellationToken);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(ApiResult<CaseLoanDetailsDto>.Ok(result));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResult<CaseLoanDetailsDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResult<CaseLoanDetailsDto>>> CreateCaseLoan(
            [FromBody] CreateCaseLoanCommand command,
            CancellationToken cancellationToken)
        {
            var created = await _mediator.Send(command, cancellationToken);
            _logger.LogInformation("Admin {AdminId} created case loan {CaseLoanId}", GetActorId(), created.Id);

            return CreatedAtAction(
                nameof(GetCaseLoanById),
                new { id = created.Id },
                ApiResult<CaseLoanDetailsDto>.Ok(created, "Case loan created successfully."));
        }

        [HttpPut("{id:guid}")]
        [HttpPost("{id:guid}")]
        [ProducesResponseType(typeof(ApiResult<CaseLoanDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResult<CaseLoanDetailsDto>>> UpdateCaseLoan(
            Guid id,
            [FromBody] UpdateCaseLoanCommand command,
            CancellationToken cancellationToken)
        {
            var updated = await _mediator.Send(command with { Id = id }, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} updated case loan {CaseLoanId}", GetActorId(), id);
            return Ok(ApiResult<CaseLoanDetailsDto>.Ok(updated, "Case loan updated successfully."));
        }

        [HttpDelete("{id:guid}")]
        [HttpPost("{id:guid}/delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCaseLoan(Guid id, CancellationToken cancellationToken)
        {
            var deleted = await _mediator.Send(new DeleteCaseLoanCommand(id), cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} deleted case loan {CaseLoanId}", GetActorId(), id);
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
