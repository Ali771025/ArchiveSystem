using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Classifications.Commands.CreateClassification;
using ArchiveSystem.Application.Features.Classifications.Commands.DeleteClassification;
using ArchiveSystem.Application.Features.Classifications.Commands.MoveClassification;
using ArchiveSystem.Application.Features.Classifications.Commands.RenameClassification;
using ArchiveSystem.Application.Features.Classifications.Models;
using ArchiveSystem.Application.Features.Classifications.Queries.GetClassificationTree;
using ArchiveSystem.Application.Features.Classifications.Queries.GetWritableClassificationIds;
using ArchiveSystem.Application.Features.Classifications.Queries.GetMovableClassificationIds;
using ArchiveSystem.Application.Features.Classifications.Queries.SearchClassifications;
using ArchiveSystem.Application.Features.DynamicFields.Commands.UpsertTemplate;
using ArchiveSystem.Application.Features.DynamicFields.Queries.GetEffectiveTemplate;
using ArchiveSystem.Application.Features.DynamicFields.Queries.GetTemplate;
using ArchiveSystem.Presentation.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/classifications")]
    public sealed class ClassificationsController : ControllerBase
    {
        private readonly ISender _sender;

        public ClassificationsController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet("tree")]
        [ProducesResponseType(typeof(IReadOnlyList<ClassificationTreeNodeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetClassificationTreeQuery(), cancellationToken));
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(IReadOnlyList<ClassificationSearchNodeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new SearchClassificationsQuery(q), cancellationToken));
        }

        [HttpGet("writable-ids")]
        [ProducesResponseType(typeof(IReadOnlyList<long>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetWritableIds(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetWritableClassificationIdsQuery(), cancellationToken));
        }

        [HttpGet("movable-ids")]
        [ProducesResponseType(typeof(IReadOnlyList<long>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMovableIds(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetMovableClassificationIdsQuery(), cancellationToken));
        }

        [HttpPost]
        [RequirePermission(Permissions.Categories.Create)]
        [ProducesResponseType(typeof(ClassificationMutationResultDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create(
            [FromBody] CreateClassificationRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new CreateClassificationCommand
            {
                Title = request.Title,
                ParentId = request.ParentId,
                Description = request.Description,
                ColorCode = request.ColorCode,
                Icon = request.Icon
            }, cancellationToken);

            return CreatedAtAction(nameof(GetTree), new { id = result.Id }, result);
        }

        [HttpPut("{id:long}")]
        [RequirePermission(Permissions.Categories.Edit)]
        [ProducesResponseType(typeof(ClassificationMutationResultDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Rename(
            long id,
            [FromBody] UpdateClassificationRequest request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new RenameClassificationCommand(id, request.Title), cancellationToken));
        }

        [HttpPost("{id:long}/rename")]
        [RequirePermission(Permissions.Categories.Edit)]
        [ProducesResponseType(typeof(ClassificationMutationResultDto), StatusCodes.Status200OK)]
        public Task<IActionResult> RenameWithPost(
            long id,
            [FromBody] UpdateClassificationRequest request,
            CancellationToken cancellationToken)
        {
            return Rename(id, request, cancellationToken);
        }

        [NonAction]
        public Task<IActionResult> Update(
            long id,
            UpdateClassificationRequest request,
            CancellationToken cancellationToken)
        {
            return Rename(id, request, cancellationToken);
        }

        [HttpPut("{id:long}/move")]
        [RequirePermission(Permissions.Categories.Manage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Move(
            long id,
            [FromBody] MoveClassificationRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new MoveClassificationCommand(id, request.NewParentId), cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        [RequirePermission(Permissions.Categories.Manage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteClassificationCommand(id), cancellationToken);
            return NoContent();
        }

        [HttpGet("{id:long}/dynamic-template")]
        [ProducesResponseType(typeof(DynamicTemplateDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDynamicTemplate(long id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetTemplateQuery(id), cancellationToken));
        }

        [HttpGet("{id:long}/effective-dynamic-template")]
        [ProducesResponseType(typeof(DynamicTemplateDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEffectiveDynamicTemplate(long id, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetEffectiveTemplateQuery(id), cancellationToken));
        }

        [HttpPut("{id:long}/dynamic-template")]
        [HttpPost("{id:long}/dynamic-template")]
        [RequirePermission(Permissions.DynamicFields.Manage)]
        [ProducesResponseType(typeof(DynamicTemplateDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertDynamicTemplate(
            long id,
            [FromBody] DynamicTemplateDto request,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new UpsertTemplateCommand
            {
                ClassificationId = id,
                Template = request
            }, cancellationToken));
        }
    }

    public sealed class CreateClassificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public long? ParentId { get; set; }
        public string? Description { get; set; }
        public string? ColorCode { get; set; }
        public string? Icon { get; set; }
    }

    public sealed class MoveClassificationRequest
    {
        public long? NewParentId { get; set; }
    }

    public sealed class UpdateClassificationRequest
    {
        public string Title { get; set; } = string.Empty;
    }
}
