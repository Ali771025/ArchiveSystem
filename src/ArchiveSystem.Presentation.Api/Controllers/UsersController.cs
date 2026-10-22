using System.Security.Claims;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Features.UserManagement.Commands;
using ArchiveSystem.Application.Features.UserManagement.Commands.CreateUser;
using ArchiveSystem.Application.Features.UserManagement.Common;
using ArchiveSystem.Application.Features.UserManagement.Queries.GetUsersWithPagination;
using ArchiveSystem.Presentation.Api.Models;
using ArchiveSystem.Presentation.Api.Models.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ArchiveSystem.Presentation.Api.Authorization;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [RequirePermission(Permissions.Users.Manage)]
    [ApiController]
    [Route("api/v1/users")]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IMediator mediator, ILogger<UsersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResult<PagedResponse<UserListItemDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<PagedResponse<UserListItemDto>>>> GetUsers(
            [FromQuery] GetUsersWithPaginationQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(ApiResult<PagedResponse<UserListItemDto>>.Ok(result));
        }

        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ApiResult<UserDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResult<UserDetailsDto>>> GetUserById(long id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
            if (result is null)
            {
                return NotFound();
            }

            return Ok(ApiResult<UserDetailsDto>.Ok(result));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResult<UserDetailsDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResult<UserDetailsDto>>> CreateUser(
            [FromBody] CreateUserRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            var created = await _mediator.Send(
                new CreateUserCommand(
                    request.Username,
                    request.Email,
                    request.PhoneNumber,
                    request.FirstName,
                    request.LastName,
                    request.NationalId,
                    request.PersonnelCode,
                    request.ActiveDuration,
                    request.AutoBackupEnabled,
                    request.TwoFactorEnabled,
                    request.Password,
                    request.Roles,
                    actorId),
                cancellationToken);

            _logger.LogInformation("Admin {AdminId} created user {UserId}", actorId, created.Id);

            return CreatedAtAction(
                nameof(GetUserById),
                new { id = created.Id },
                ApiResult<UserDetailsDto>.Ok(created, "User created successfully."));
        }

        [HttpPut("{id:long}")]
        [ProducesResponseType(typeof(ApiResult<UserDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResult<UserDetailsDto>>> UpdateUser(
            long id,
            [FromBody] UpdateUserRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            var updated = await _mediator.Send(
                new UpdateUserCommand(
                    id,
                    request.Username,
                    request.Email,
                    request.PhoneNumber,
                    request.FirstName,
                    request.LastName,
                    request.NationalId,
                    request.PersonnelCode,
                    request.ActiveDuration,
                    request.AutoBackupEnabled,
                    request.TwoFactorEnabled,
                    actorId),
                cancellationToken);

            if (updated is null)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} updated user {UserId}", actorId, id);
            return Ok(ApiResult<UserDetailsDto>.Ok(updated, "User updated successfully."));
        }

        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(long id, CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            var exists = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
            if (exists is null)
            {
                return NotFound();
            }

            var changed = await _mediator.Send(new ToggleUserStatusCommand(id, false, actorId), cancellationToken);
            if (!changed)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} deactivated user {UserId}", actorId, id);
            return NoContent();
        }

        [HttpPut("{id:long}/status")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleUserStatus(
            long id,
            [FromBody] ToggleUserStatusRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            var changed = await _mediator.Send(
                new ToggleUserStatusCommand(id, request.IsActive, actorId),
                cancellationToken);

            if (!changed)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} changed status for User {UserId} to {IsActive}", actorId, id, request.IsActive);
            return NoContent();
        }

        [HttpPut("{id:long}/password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(
            long id,
            [FromBody] ResetUserPasswordRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            var changed = await _mediator.Send(
                new ResetUserPasswordCommand(id, request.NewPassword, actorId),
                cancellationToken);

            if (!changed)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} reset password for User {UserId}", actorId, id);
            return NoContent();
        }

        [HttpPut("{id:long}/roles")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AssignRoles(
            long id,
            [FromBody] AssignRolesRequest request,
            CancellationToken cancellationToken)
        {
            var actorId = GetActorId();
            var changed = await _mediator.Send(
                new AssignRolesCommand(id, request.Roles, actorId),
                cancellationToken);

            if (!changed)
            {
                return NotFound();
            }

            _logger.LogInformation("Admin {AdminId} changed roles for User {UserId}", actorId, id);
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
