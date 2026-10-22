using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Features.Files.Security;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Files.Commands.SetFileTags
{
    public sealed class SetFileTagsCommandHandler : IRequestHandler<SetFileTagsCommand, Unit>
    {
        private readonly IFileTagRepository _fileTagRepository;
        private readonly IAccessControlLookupRepository _lookupRepository;
        private readonly IFileAccessAuthorizer _fileAccessAuthorizer;
        private readonly IAuditWriter _auditWriter;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SetFileTagsCommandHandler(
            IFileTagRepository fileTagRepository,
            IAccessControlLookupRepository lookupRepository,
            IFileAccessAuthorizer fileAccessAuthorizer,
            IAuditWriter auditWriter,
            IHttpContextAccessor httpContextAccessor)
        {
            _fileTagRepository = fileTagRepository;
            _lookupRepository = lookupRepository;
            _fileAccessAuthorizer = fileAccessAuthorizer;
            _auditWriter = auditWriter;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Unit> Handle(SetFileTagsCommand request, CancellationToken cancellationToken)
        {
            var distinctTagIds = request.TagIds.Distinct().ToArray();
            await _fileAccessAuthorizer.EnsureCanAssignTagsAsync(request.FileId, distinctTagIds, cancellationToken);

            var descriptor = await _lookupRepository.GetFileAccessDescriptorAsync(request.FileId, cancellationToken);
            var previousTagIds = await _fileTagRepository.GetTagIdsByFileIdAsync(request.FileId, cancellationToken);

            await _fileTagRepository.SetFileTagsAsync(request.FileId, distinctTagIds, cancellationToken);

            foreach (var tagId in distinctTagIds.Except(previousTagIds))
            {
                await WriteAuditAsync("FILE_TAG_ASSIGNED", request.FileId, tagId, descriptor?.ClassificationId, cancellationToken);
            }

            foreach (var tagId in previousTagIds.Except(distinctTagIds))
            {
                await WriteAuditAsync("FILE_TAG_REMOVED", request.FileId, tagId, descriptor?.ClassificationId, cancellationToken);
            }

            return Unit.Value;
        }

        private Task WriteAuditAsync(
            string actionType,
            long fileId,
            long tagId,
            long? classificationId,
            CancellationToken cancellationToken)
        {
            return _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = _fileAccessAuthorizer.CurrentUserId,
                ActorName = _httpContextAccessor.HttpContext?.User.Identity?.Name,
                ActionType = actionType,
                EntityType = "FileTag",
                EntityId = $"{fileId}:{tagId}",
                ClassificationId = classificationId,
                FileId = fileId,
                TagId = tagId,
                AfterJson = System.Text.Json.JsonSerializer.Serialize(new { fileId, tagId }),
                Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString()
            }, cancellationToken);
        }
    }
}
