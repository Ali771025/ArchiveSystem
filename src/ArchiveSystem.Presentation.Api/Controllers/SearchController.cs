using System.Security.Claims;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Features.Search.Documents;
using ArchiveSystem.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/v1/search")]
    public class SearchController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAccessControlService _accessControlService;

        public SearchController(
            IMediator mediator,
            IAccessControlService accessControlService)
        {
            _mediator = mediator;
            _accessControlService = accessControlService;
        }

        [HttpPost("documents")]
        [ProducesResponseType(typeof(AdvancedDocumentSearchResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchDocuments(
            [FromBody] AdvancedDocumentSearchRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var bypassAuthorizationFilters = User.IsInRole("Admin")
                || await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);

            var allowedClassificationIds = await _accessControlService.GetAccessibleClassificationIdsAsync(
                userId,
                ClassificationPermissionMask.Read,
                cancellationToken);

            var result = await _mediator.Send(new AdvancedDocumentSearchQuery
            {
                Request = request,
                AllowedClassificationIds = allowedClassificationIds,
                BypassAuthorizationFilters = bypassAuthorizationFilters,
            }, cancellationToken);

            return Ok(result);
        }

        private long GetUserId()
        {
            var raw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var userId) ? userId : 0;
        }
    }
}
