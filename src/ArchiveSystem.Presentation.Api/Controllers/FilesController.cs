using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Files.Commands.DeleteFile;
using ArchiveSystem.Application.Features.Files.Commands.SetFileTags;
using ArchiveSystem.Application.Features.Files.Commands.RetryFileOcr;
using ArchiveSystem.Application.Features.Files.Queries.GetFileById;
using ArchiveSystem.Application.Features.Files.Queries.GetAssignableTags;
using ArchiveSystem.Application.Features.Files.Queries.SearchFiles;
using ArchiveSystem.Application.Features.Files.UploadPipeline.Commands;
using ArchiveSystem.Application.Features.Files.UploadPipeline.Queries;
using ArchiveSystem.Presentation.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/files")]
    public sealed class FilesController : ControllerBase
    {
        private readonly ISender _sender;

        public FilesController(ISender sender)
        {
            _sender = sender;
        }

        [HttpGet]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(typeof(PaginatedResult<FileDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFiles([FromQuery] SearchFilesQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(query, cancellationToken));
        }

        [HttpGet("{fileId:long}")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(typeof(FileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFileById(long fileId, CancellationToken cancellationToken)
        {
            var file = await _sender.Send(new GetFileByIdQuery(fileId), cancellationToken);
            return file is null ? NotFound() : Ok(file);
        }

        [HttpGet("tags/assignable")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(typeof(IReadOnlyList<AssignableTagDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAssignableTags(CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetAssignableTagsQuery(), cancellationToken));
        }

        [HttpPost("uploads/initiate")]
        [RequirePermission(Permissions.Files.Manage)]
        [ProducesResponseType(typeof(InitiateUploadSessionResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<InitiateUploadSessionResponse>> InitiateUpload(
            [FromBody] InitiateUploadSessionCommand command,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(command, cancellationToken));
        }

        [HttpPut("uploads/{sessionId:guid}/parts/{partNumber:int}")]
        [RequirePermission(Permissions.Files.Manage)]
        [Consumes("application/octet-stream")]
        [ProducesResponseType(typeof(UploadPartResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UploadPartResponse>> UploadPart(
            Guid sessionId,
            int partNumber,
            CancellationToken cancellationToken)
        {
            var contentLength = Request.ContentLength ?? 0;
            if (contentLength <= 0)
            {
                return BadRequest("Request body must contain chunk bytes.");
            }

            return Ok(await _sender.Send(new UploadPartCommand
            {
                SessionId = sessionId,
                PartNumber = partNumber,
                Content = Request.Body,
                ContentLength = contentLength
            }, cancellationToken));
        }

        [HttpPost("uploads/{sessionId:guid}/complete")]
        [RequirePermission(Permissions.Files.Manage)]
        [ProducesResponseType(typeof(FinalizeUploadResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<FinalizeUploadResponse>> CompleteUpload(
            Guid sessionId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new FinalizeUploadCommand { SessionId = sessionId }, cancellationToken));
        }

        [HttpPost("uploads/{sessionId:guid}/abort")]
        [RequirePermission(Permissions.Files.Manage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AbortUpload(
            Guid sessionId,
            [FromBody] AbortUploadRequest? request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new AbortUploadCommand
            {
                SessionId = sessionId,
                Reason = request?.Reason
            }, cancellationToken);

            return NoContent();
        }

        [HttpGet("{fileId:long}/versions")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(typeof(IReadOnlyList<FileVersionDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<FileVersionDto>>> ListVersions(
            long fileId,
            CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new ListVersionsQuery { FileId = fileId }, cancellationToken));
        }

        [HttpGet("{fileId:long}/versions/{versionId:long}/thumbnail")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVersionThumbnail(
            long fileId,
            long versionId,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetFileThumbnailQuery { FileId = fileId, VersionId = versionId }, cancellationToken);
            return result is null ? NotFound() : File(result.Content, result.ContentType);
        }

        [HttpGet("{fileId:long}/versions/{versionId:long}/ocr")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVersionOcr(
            long fileId,
            long versionId,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetFileOcrQuery { FileId = fileId, VersionId = versionId }, cancellationToken);
            return result is null ? NotFound() : Content(result, "application/json");
        }

        [HttpPost("{fileId:long}/versions/{versionId:long}/ocr/retry")]
        [RequirePermission(Permissions.Files.Manage)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RetryVersionOcr(
            long fileId,
            long versionId,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new RetryFileOcrCommand(fileId, versionId), cancellationToken);
            return Accepted();
        }

        [HttpGet("{fileId:long}/latest/thumbnail")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestThumbnail(long fileId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetFileThumbnailQuery { FileId = fileId }, cancellationToken);
            return result is null ? NotFound() : File(result.Content, result.ContentType);
        }

        [HttpGet("{fileId:long}/latest/ocr")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestOcr(long fileId, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetFileOcrQuery { FileId = fileId }, cancellationToken);
            return result is null ? NotFound() : Content(result, "application/json");
        }

        [HttpGet("{fileId:long}/download")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadLatest(long fileId, CancellationToken cancellationToken)
        {
            return await DownloadInternal(fileId, null, cancellationToken);
        }

        [HttpGet("{fileId:long}/versions/{versionId:long}/download")]
        [RequirePermission(Permissions.Files.Read)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadVersion(long fileId, long versionId, CancellationToken cancellationToken)
        {
            return await DownloadInternal(fileId, versionId, cancellationToken);
        }

        [HttpDelete("{fileId:long}/versions/{versionId:long}")]
        [RequirePermission(Permissions.Files.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SoftDeleteVersion(long fileId, long versionId, CancellationToken cancellationToken)
        {
            await _sender.Send(new SoftDeleteFileVersionCommand { FileId = fileId, VersionId = versionId }, cancellationToken);
            return NoContent();
        }

        [HttpDelete("{fileId:long}")]
        [RequirePermission(Permissions.Files.Delete)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SoftDeleteFile(long fileId, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteFileCommand(fileId), cancellationToken);
            return NoContent();
        }

        [HttpPut("{fileId:long}/tags")]
        [RequirePermission(Permissions.Files.Manage)]
        [RequirePermission(Permissions.Tags.Manage)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SetFileTags(
            long fileId,
            [FromBody] UpdateFileTagsRequest request,
            CancellationToken cancellationToken)
        {
            await _sender.Send(new SetFileTagsCommand
            {
                FileId = fileId,
                TagIds = request.TagIds ?? Array.Empty<long>()
            }, cancellationToken);

            return NoContent();
        }

        private async Task<IActionResult> DownloadInternal(
            long fileId,
            long? versionId,
            CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetDownloadQuery { FileId = fileId, VersionId = versionId }, cancellationToken);
            return result is null ? NotFound() : File(result.Content, result.ContentType, result.FileName);
        }
    }

    public sealed class AbortUploadRequest
    {
        public string? Reason { get; set; }
    }

    public sealed class UpdateFileTagsRequest
    {
        public IReadOnlyCollection<long>? TagIds { get; set; }
    }
}
