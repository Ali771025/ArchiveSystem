using System.Security.Claims;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Features.Reports.Commands.CreateActivityExcelExportJob;
using ArchiveSystem.Application.Features.Reports.Commands.CreateFilesZipExportJob;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Application.Features.Reports.Queries.DownloadJobResult;
using ArchiveSystem.Application.Features.Reports.Queries.GetJobStatus;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/exports")]
    public class ExportsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAccessControlService _accessControlService;

        public ExportsController(IMediator mediator, IAccessControlService accessControlService)
        {
            _mediator = mediator;
            _accessControlService = accessControlService;
        }

        [HttpPost("activity/excel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateActivityExcelExport(
            [FromBody] ActivityExcelExportRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CreateActivityExcelExportJobCommand
            {
                UserId = GetUserId(),
                Request = request
            }, cancellationToken);

            return Ok(result);
        }

        [HttpPost("files/zip")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateFilesZipExport(
            [FromBody] FilesZipExportRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new CreateFilesZipExportJobCommand
            {
                UserId = GetUserId(),
                Request = request
            }, cancellationToken);

            return Ok(result);
        }

        [HttpGet("jobs/{jobId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJobStatus(Guid jobId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetJobStatusQuery
            {
                JobId = jobId,
                RequesterUserId = GetUserId(),
                AllowManageAll = await CanManageAllJobsAsync(cancellationToken)
            }, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("jobs/{jobId:guid}/download")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadJobResult(Guid jobId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new DownloadJobResultQuery
            {
                JobId = jobId,
                RequesterUserId = GetUserId(),
                AllowManageAll = await CanManageAllJobsAsync(cancellationToken)
            }, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return File(result.Content, result.ContentType, result.FileName);
        }

        private async Task<bool> CanManageAllJobsAsync(CancellationToken cancellationToken)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            return await _accessControlService.HasGlobalPermissionAsync(GetUserId(), "admin.access", cancellationToken);
        }

        private long GetUserId()
        {
            var raw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var userId) ? userId : 0;
        }
    }
}
