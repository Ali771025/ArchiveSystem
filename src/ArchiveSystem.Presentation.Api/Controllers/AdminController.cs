using System.Security.Claims;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Infrastructure.Identity;
using ArchiveSystem.Presentation.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAccessAdministrationService _administrationService;
        private readonly IAccessControlService _accessControlService;
        private readonly ITagRepository _tagRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditWriter _auditWriter;
        private readonly ArchiveSystem.Application.Common.Interfaces.Security.IIdentityManagementService _identityManagementService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAccessAdministrationService administrationService,
            IAccessControlService accessControlService,
            ITagRepository tagRepository,
            UserManager<ApplicationUser> userManager,
            IAuditWriter auditWriter,
            ArchiveSystem.Application.Common.Interfaces.Security.IIdentityManagementService identityManagementService,
            ILogger<AdminController> logger)
        {
            _administrationService = administrationService;
            _accessControlService = accessControlService;
            _tagRepository = tagRepository;
            _userManager = userManager;
            _auditWriter = auditWriter;
            _identityManagementService = identityManagementService;
            _logger = logger;
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            return Ok(await _administrationService.GetRolesAsync(cancellationToken));
        }

        [HttpPost("roles")]
        public async Task<IActionResult> CreateRole([FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var role = await _administrationService.CreateRoleAsync(request.Name, request.IsSystem, cancellationToken);
            return CreatedAtAction(nameof(GetRoles), new { id = role.Id }, role);
        }

        [HttpPut("roles/{id:long}")]
        public async Task<IActionResult> UpdateRole(long id, [FromBody] UpsertRoleRequest request, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var role = await _administrationService.UpdateRoleAsync(id, request.Name, cancellationToken);
            return role == null ? NotFound() : Ok(role);
        }

        [HttpDelete("roles/{id:long}")]
        public async Task<IActionResult> DeleteRole(long id, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var deleted = await _administrationService.DeleteRoleAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var permissions = await _administrationService.GetPermissionsAsync(cancellationToken);
            return Ok(permissions);
        }

        [HttpGet("permissions/definitions")]
        public async Task<IActionResult> GetPermissionDefinitions(CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            return Ok(Permissions.GetAll());
        }

        [HttpGet("roles/{id:long}/permissions")]
        public async Task<IActionResult> GetRolePermissions(long id, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var permissions = await _administrationService.GetRolePermissionIdsAsync(id, cancellationToken);
            return Ok(permissions);
        }

        [HttpPut("roles/{id:long}/permissions")]
        public async Task<IActionResult> SetRolePermissions(long id, [FromBody] SetRolePermissionsRequest request, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            await _administrationService.SetRolePermissionsAsync(id, request.PermissionIds ?? Array.Empty<long>(), cancellationToken);
            return NoContent();
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var users = _userManager.Users
                .Select(x => new AdminUserDto
                {
                    Id = x.Id,
                    UserName = x.UserName ?? string.Empty,
                    Email = x.Email ?? string.Empty,
                    FirstName = x.FirstName,
                    LastName = x.LastName
                })
                .ToList();

            return Ok(users);
        }

        [HttpPut("users/{id:long}/roles")]
        public async Task<IActionResult> SetUserRoles(long id, [FromBody] SetUserRolesRequest request, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var beforeRoles = await _administrationService.GetUserRolesAsync(id, cancellationToken);
            await _administrationService.SetUserRolesAsync(id, request.RoleIds ?? Array.Empty<long>(), cancellationToken);
            var afterRoles = await _administrationService.GetUserRolesAsync(id, cancellationToken);

            var beforeSet = beforeRoles.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var afterSet = afterRoles.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var addedRoles = afterSet.Except(beforeSet).OrderBy(x => x).ToArray();
            var removedRoles = beforeSet.Except(afterSet).OrderBy(x => x).ToArray();

            _logger.LogInformation(
                "User roles updated by {ActorUserId}. Actor={ActorName}, TargetUserId={TargetUserId}, AddedRoles=[{AddedRoles}], RemovedRoles=[{RemovedRoles}], FinalRoles=[{FinalRoles}]",
                GetUserId(),
                User.Identity?.Name ?? "unknown",
                id,
                string.Join(", ", addedRoles),
                string.Join(", ", removedRoles),
                string.Join(", ", afterSet.OrderBy(x => x)));

            return NoContent();
        }

        [HttpGet("users/{id:long}/roles")]
        public async Task<IActionResult> GetUserRoles(long id, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            return Ok(await _administrationService.GetUserRolesAsync(id, cancellationToken));
        }

        [HttpGet("identity/roles/{roleName}/permissions")]
        public async Task<IActionResult> GetIdentityRolePermissions(string roleName, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var claims = await _identityManagementService.GetRolePermissionClaimsAsync(roleName, cancellationToken);
            return Ok(claims);
        }

        [HttpPut("identity/roles/{roleName}/permissions")]
        public async Task<IActionResult> UpdateIdentityRolePermissions(
            string roleName,
            [FromBody] UpdateIdentityRolePermissionClaimsRequest request,
            CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var result = await _identityManagementService.UpdateRolePermissionClaimsAsync(
                roleName,
                request.Permissions ?? Array.Empty<string>(),
                cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors });
            }

            return NoContent();
        }

        [HttpGet("identity/users/{id:long}/roles")]
        public async Task<IActionResult> GetIdentityUserRoles(long id, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var roles = await _identityManagementService.GetUserRolesAsync(id, cancellationToken);
            return Ok(roles);
        }

        [HttpPut("identity/users/{id:long}/roles")]
        [HttpPost("identity/users/{id:long}/roles")]
        public async Task<IActionResult> UpdateIdentityUserRoles(
            long id,
            [FromBody] UpdateIdentityUserRolesRequest request,
            CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var result = await _identityManagementService.UpdateUserRolesAsync(
                id,
                request.RoleNames ?? Array.Empty<string>(),
                cancellationToken);

            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors });
            }

            _accessControlService.InvalidateUserCache(id);
            return NoContent();
        }

        [HttpGet("classifications/{id:long}/acl")]
        public async Task<IActionResult> GetClassificationAcl(
            long id,
            [FromQuery] bool includeDescendants,
            CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            return Ok(await _administrationService.GetClassificationAclAsync(id, includeDescendants, cancellationToken));
        }

        [HttpPut("classifications/{id:long}/acl")]
        [HttpPost("classifications/{id:long}/acl")]
        public async Task<IActionResult> UpdateClassificationAcl(
            long id,
            [FromBody] UpdateClassificationAclRequest request,
            CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var before = await _administrationService.GetClassificationAclAsync(id, cancellationToken: cancellationToken);
            await _administrationService.UpdateClassificationAclAsync(id, request.Entries ?? Array.Empty<ClassificationAclEntryDto>(), cancellationToken);
            var after = await _administrationService.GetClassificationAclAsync(id, cancellationToken: cancellationToken);

            await _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = GetUserId(),
                ActorName = User.Identity?.Name,
                ActionType = "CLASSIFICATION_ACL_CHANGED",
                EntityType = "ClassificationAcl",
                EntityId = id.ToString(),
                ClassificationId = id,
                BeforeJson = System.Text.Json.JsonSerializer.Serialize(before),
                AfterJson = System.Text.Json.JsonSerializer.Serialize(after),
                Ip = GetIpAddress(),
                UserAgent = GetUserAgent()
            }, cancellationToken);

            return NoContent();
        }

        [HttpGet("tags")]
        public async Task<IActionResult> GetTags(CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var tags = await _tagRepository.GetAllAsync(cancellationToken);
            return Ok(tags.Select(x => new TagDto(x.Id, x.Name, x.Description)));
        }

        [HttpPost("tags")]
        [RequirePermission(Permissions.Tags.Manage)]
        public async Task<IActionResult> CreateTag([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
        {
            var normalizedName = request.Name is null
                ? null
                : ArchiveSystem.Domain.Common.PersianTextNormalizer.CleanTagName(request.Name);
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return BadRequest("Tag name is required.");
            }

            if (normalizedName.Length > 120 || request.Description?.Trim().Length > 500)
            {
                return BadRequest("Tag name or description exceeds the allowed length.");
            }

            var existingTag = await _tagRepository.GetByNameAsync(
                normalizedName,
                cancellationToken);
            if (existingTag is not null)
            {
                return Ok(new TagDto(
                    existingTag.Id,
                    existingTag.Name,
                    existingTag.Description));
            }

            var tag = new Tag(0, normalizedName, request.Description);
            await _tagRepository.AddAsync(tag, cancellationToken);
            await _tagRepository.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(GetTags), new { id = tag.Id }, new TagDto(tag.Id, tag.Name, tag.Description));
        }

        [HttpGet("tags/acl")]
        public async Task<IActionResult> GetTagAcl([FromQuery] long tagId, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            return Ok(await _administrationService.GetTagAclAsync(tagId, cancellationToken));
        }

        [HttpPut("tags/acl")]
        public async Task<IActionResult> UpdateTagAcl([FromBody] UpdateTagAclRequest request, CancellationToken cancellationToken)
        {
            if (!await HasAdminAccessAsync(cancellationToken))
            {
                return Forbid();
            }

            var before = await _administrationService.GetTagAclAsync(request.TagId, cancellationToken);
            await _administrationService.UpdateTagAclAsync(request.TagId, request.Entries ?? Array.Empty<TagAclEntryDto>(), cancellationToken);
            var after = await _administrationService.GetTagAclAsync(request.TagId, cancellationToken);

            await _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = GetUserId(),
                ActorName = User.Identity?.Name,
                ActionType = "TAG_ACL_CHANGED",
                EntityType = "TagAcl",
                EntityId = request.TagId.ToString(),
                TagId = request.TagId,
                BeforeJson = System.Text.Json.JsonSerializer.Serialize(before),
                AfterJson = System.Text.Json.JsonSerializer.Serialize(after),
                Ip = GetIpAddress(),
                UserAgent = GetUserAgent()
            }, cancellationToken);

            return NoContent();
        }

        private async Task<bool> HasAdminAccessAsync(CancellationToken cancellationToken)
        {
            if (User.IsInRole("Admin") || User.IsInRole("Superuser"))
            {
                return true;
            }

            var userId = GetUserId();
            if (userId <= 0)
            {
                return false;
            }

            return await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken)
                   || await _identityManagementService.HasUserPermissionAsync(
                       userId,
                       Permissions.Roles.Manage,
                       cancellationToken);
        }

        private long GetUserId()
        {
            var raw = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var userId) ? userId : 0;
        }

        private string GetIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].ToString();
        }
    }

    public sealed class UpsertRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
    }

    public sealed class SetRolePermissionsRequest
    {
        public IReadOnlyCollection<long>? PermissionIds { get; set; }
    }

    public sealed class SetUserRolesRequest
    {
        public IReadOnlyCollection<long>? RoleIds { get; set; }
    }

    public sealed class UpdateIdentityRolePermissionClaimsRequest
    {
        public IReadOnlyCollection<string>? Permissions { get; set; }
    }

    public sealed class UpdateIdentityUserRolesRequest
    {
        public IReadOnlyCollection<string>? RoleNames { get; set; }
    }

    public sealed class UpdateClassificationAclRequest
    {
        public IReadOnlyCollection<ClassificationAclEntryDto>? Entries { get; set; }
    }

    public sealed class UpdateTagAclRequest
    {
        public long TagId { get; set; }
        public IReadOnlyCollection<TagAclEntryDto>? Entries { get; set; }
    }

    public sealed class CreateTagRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public sealed class AdminUserDto
    {
        public long Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    public sealed record TagDto(long Id, string Name, string? Description);
}
