using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Features.Dossiers.AdvancedSearch;
using ArchiveSystem.Application.Features.Dossiers.Models;
using ArchiveSystem.Application.Features.Dossiers.Ownership;
using ArchiveSystem.Application.Features.Taxpayers.DTOs;
using ArchiveSystem.Application.Features.Taxpayers.Queries.GetPagedTaxpayers;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Presentation.Api.Authorization;
using ArchiveSystem.Presentation.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[RequirePermission(Permissions.Dossiers.Manage)]
[ApiController]
[Route("api/dossiers")]
public sealed class DossiersController : ControllerBase
{
    private readonly ISender _sender;

    public DossiersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("owner-candidates")]
    [ProducesResponseType(typeof(ApiResult<PagedResponse<TaxpayerListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResult<PagedResponse<TaxpayerListItemDto>>>> GetOwnerCandidates(
        [FromQuery] GetPagedTaxpayersQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(query, cancellationToken);
        return Ok(ApiResult<PagedResponse<TaxpayerListItemDto>>.Ok(result));
    }

    [HttpPost("advanced-search")]
    [Obsolete("Deprecated. Use POST /api/v1/search/documents instead.")]
    [ProducesResponseType(typeof(DossierAdvancedSearchResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdvancedSearch(
        [FromBody] DossierAdvancedSearchRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SearchDossiersAdvancedQuery
        {
            Request = request
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("advanced-search/fields")]
    [ProducesResponseType(typeof(DossierAdvancedSearchFieldCatalogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdvancedSearchFields(
        [FromQuery] long? classificationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDossierAdvancedSearchFieldsQuery
        {
            ClassificationId = classificationId
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{dossierId:long}/owners")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DossierOwnerItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOwners(long dossierId, CancellationToken cancellationToken)
    {
        var owners = await _sender.Send(new GetDossierOwnersQuery(dossierId, null), cancellationToken);
        return Ok(owners);
    }

    [HttpGet("by-case-number/{caseNumber}/owners")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DossierOwnerItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOwnersByCaseNumber(string caseNumber, CancellationToken cancellationToken)
    {
        var owners = await _sender.Send(new GetDossierOwnersQuery(null, caseNumber), cancellationToken);
        return Ok(owners);
    }

    [HttpPost("{dossierId:long}/owners")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AttachOwner(
        long dossierId,
        [FromBody] AttachDossierOwnerRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AttachDossierOwnerCommand(dossierId, null, request.OwnerId, request.TaxpayerId), cancellationToken);
        return NoContent();
    }

    [HttpPost("by-case-number/{caseNumber}/owners")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AttachOwnerByCaseNumber(
        string caseNumber,
        [FromBody] AttachDossierOwnerRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new AttachDossierOwnerCommand(null, caseNumber, request.OwnerId, request.TaxpayerId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{dossierId:long}/owners/{ownerId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetachOwner(long dossierId, long ownerId, CancellationToken cancellationToken)
    {
        await _sender.Send(new DetachDossierOwnerCommand(dossierId, null, ownerId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("by-case-number/{caseNumber}/owners/{ownerId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetachOwnerByCaseNumber(
        string caseNumber,
        long ownerId,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DetachDossierOwnerCommand(null, caseNumber, ownerId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{dossierId:long}/avatar")]
    [HttpPost("~/api/v1/dossiers/{dossierId:long}/avatar")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [ProducesResponseType(typeof(DossierAvatarUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadAvatar(
        long dossierId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UploadDossierAvatarCommand(dossierId, file), cancellationToken);
        return Ok(result);
    }
}
