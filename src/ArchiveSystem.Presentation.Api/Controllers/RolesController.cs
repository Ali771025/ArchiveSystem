using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Features.AccessControl.Commands.CreateRole;
using ArchiveSystem.Application.Features.AccessControl.Commands.DeleteRole;
using ArchiveSystem.Application.Features.AccessControl.Commands.SetUserRoles;
using ArchiveSystem.Application.Features.AccessControl.Commands.UpdateRole;
using ArchiveSystem.Application.Features.AccessControl.DTOs;
using ArchiveSystem.Application.Features.AccessControl.Queries.GetRoleById;
using ArchiveSystem.Application.Features.AccessControl.Queries.GetRoles;
using ArchiveSystem.Application.Features.AccessControl.Queries.GetUserRoles;
using ArchiveSystem.Presentation.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize]
    [RequirePermission(Permissions.Roles.Manage)]
    [ApiController]
    [Route("api/v1")]
    public sealed class RolesController : ControllerBase
    {
        private readonly ISender _sender;

        public RolesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("roles")]
        [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetRolesQuery(), cancellationToken));
        }

        [HttpGet("roles/{id:long}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDto>> GetRole(long id, CancellationToken cancellationToken)
        {
            var role = await _sender.Send(new GetRoleByIdQuery(id), cancellationToken);
            return role is null ? NotFound() : Ok(role);
        }

        [HttpPost("roles")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] RoleUpsertRequest request, CancellationToken cancellationToken)
        {
            var role = await _sender.Send(new CreateRoleCommand(request.Name, request.IsSystem), cancellationToken);
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
        }

        [HttpPut("roles/{id:long}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<RoleDto>> UpdateRole(long id, [FromBody] RoleUpsertRequest request, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UpdateRoleCommand(id, request.Name), cancellationToken));
        }

        [HttpDelete("roles/{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteRole(long id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteRoleCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpGet("users/{userId:long}/roles")]
        [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetUserRoles(long userId, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetUserRolesQuery(userId), cancellationToken));
        }

        [HttpPut("role-assignments/users/{userId:long}/roles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AssignRolesToUser(
            long userId,
            [FromBody] AssignRolesToUserRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new SetUserRolesCommand(userId, request.RoleIds ?? Array.Empty<long>()), cancellationToken);
            return NoContent();
        }
    }

    public sealed class RoleUpsertRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }

    public sealed class AssignRolesToUserRequest
    {
        public IReadOnlyCollection<long>? RoleIds { get; set; }
    }
}
