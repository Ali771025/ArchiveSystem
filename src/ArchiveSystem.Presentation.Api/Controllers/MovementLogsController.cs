using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Models.MovementLogs;
using ArchiveSystem.Application.Features.MovementLogs.Commands.CreatePhysicalMovementLog;
using ArchiveSystem.Application.Features.MovementLogs.Queries.GetPhysicalMovementLogById;
using ArchiveSystem.Presentation.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[RequirePermission(Permissions.Dossiers.Manage)]
[ApiController]
[Route("api/movement-logs")]
public sealed class MovementLogsController : ControllerBase
{
    private readonly ISender _sender;

    public MovementLogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MovementLogResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMovementLog(
        [FromBody] CreateMovementLogDto dto,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new CreatePhysicalMovementLogCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetMovementLogById), new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MovementLogResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMovementLogById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetPhysicalMovementLogByIdQuery(id), cancellationToken);
        return response == null ? NotFound() : Ok(response);
    }
}
