using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Owners.Commands.CreateOwner;
using ArchiveSystem.Application.Features.Owners.Commands.UpdateOwner;
using ArchiveSystem.Application.Features.Owners.Models;
using ArchiveSystem.Application.Features.Owners.Queries.GetOwnerById;
using ArchiveSystem.Application.Features.Owners.Queries.GetOwners;
using ArchiveSystem.Application.Features.Owners.Queries.SearchOwners;
using ArchiveSystem.Presentation.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[RequirePermission(Permissions.Dossiers.Manage)]
[ApiController]
[Route("api/owners")]
public sealed class OwnersController : ControllerBase
{
    private readonly ISender _sender;

    public OwnersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [RequirePermission(Permissions.Taxpayers.Manage)]
    [ProducesResponseType(typeof(OwnerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOwner(
        [FromBody] CreateOwnerDto request,
        CancellationToken cancellationToken)
    {
        var owner = await _sender.Send(new CreateOwnerCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetOwnerById), new { ownerId = owner.Id }, owner);
    }

    [HttpGet("{ownerId:long}")]
    [ProducesResponseType(typeof(OwnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOwnerById(long ownerId, CancellationToken cancellationToken)
    {
        var owner = await _sender.Send(new GetOwnerByIdQuery(ownerId), cancellationToken);
        return owner == null ? NotFound() : Ok(owner);
    }

    [HttpPut("{ownerId:long}")]
    [RequirePermission(Permissions.Taxpayers.Manage)]
    [ProducesResponseType(typeof(OwnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOwner(
        long ownerId,
        [FromBody] CreateOwnerDto request,
        CancellationToken cancellationToken)
    {
        var owner = await _sender.Send(new UpdateOwnerCommand(ownerId, request), cancellationToken);
        return Ok(owner);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<OwnerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOwners(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetOwnersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OwnerSearchItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchOwners(
        [FromQuery] string? query,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchOwnersQuery(query, take), cancellationToken);
        return Ok(result);
    }
}
