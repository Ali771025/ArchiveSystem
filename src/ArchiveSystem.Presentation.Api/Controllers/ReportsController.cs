using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Features.Reports.Queries.GetAuditTrail;
using ArchiveSystem.Application.Features.Reports.Queries.GetAuditFilterOptions;
using ArchiveSystem.Application.Features.Reports.Queries.GetSecurityLogs;
using ArchiveSystem.Presentation.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Auditor,Superuser")]
[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("audit-trail")]
    [ProducesResponseType(typeof(ApiResult<PagedResultDto<AuditLogResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<PagedResultDto<AuditLogResponseDto>>>> GetAuditTrail(
        [FromQuery] AuditFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuditTrailQuery(filter), cancellationToken);
        return Ok(ApiResult<PagedResultDto<AuditLogResponseDto>>.Ok(result));
    }

    [HttpGet("audit-filter-options")]
    [ProducesResponseType(typeof(ApiResult<AuditFilterOptionsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<AuditFilterOptionsDto>>> GetAuditFilterOptions(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuditFilterOptionsQuery(), cancellationToken);
        return Ok(ApiResult<AuditFilterOptionsDto>.Ok(result));
    }

    [HttpGet("auth-logs")]
    [ProducesResponseType(typeof(ApiResult<PagedResultDto<SecurityLogResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResult<PagedResultDto<SecurityLogResponseDto>>>> GetAuthenticationLogs(
        [FromQuery] SecurityLogFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSecurityLogsQuery(filter), cancellationToken);
        return Ok(ApiResult<PagedResultDto<SecurityLogResponseDto>>.Ok(result));
    }
}
