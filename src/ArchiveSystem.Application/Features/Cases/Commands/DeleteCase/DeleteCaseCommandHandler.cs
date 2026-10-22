using System.Text.Json;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Commands.DeleteCase
{
    public sealed class DeleteCaseCommandHandler : IRequestHandler<DeleteCaseCommand, DeleteCaseResult>
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IClassificationRepository _classificationRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IAuditWriter _auditWriter;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public DeleteCaseCommandHandler(
            ICaseRepository caseRepository,
            IClassificationRepository classificationRepository,
            IAccessControlService accessControlService,
            IAuditWriter auditWriter,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _caseRepository = caseRepository;
            _classificationRepository = classificationRepository;
            _accessControlService = accessControlService;
            _auditWriter = auditWriter;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<DeleteCaseResult> Handle(DeleteCaseCommand request, CancellationToken cancellationToken)
        {
            var entity = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken);
            if (entity == null)
            {
                throw new NotFoundException($"Case '{request.CaseId}' not found.");
            }

            var userId = ParseUserId();
            var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true ||
                          await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);

            if (!isAdmin && !await _accessControlService.CanWriteClassificationAsync(userId, entity.ClassificationId, cancellationToken))
            {
                throw new ForbiddenAccessException(
                    "شما فقط مجوز مشاهده این طبقه‌بندی را دارید و امکان بایگانی پرونده برای شما فعال نیست.");
            }

            var archiveClassification = await _classificationRepository.GetBySystemKeyAsync(
                SystemClassificationKeys.ArchivedCases,
                cancellationToken);
            if (archiveClassification == null || !archiveClassification.IsActive)
            {
                throw new NotFoundException("System archive classification 'ArchivedCases' not found.");
            }

            var sourceClassificationId = entity.ClassificationId;
            if (sourceClassificationId != archiveClassification.Id)
            {
                var moved = await _caseRepository.MoveToClassificationAsync(
                    entity.Id,
                    archiveClassification.Id,
                    cancellationToken);

                if (!moved)
                {
                    throw new ConflictException($"Case '{entity.Id}' could not be archived.");
                }
            }

            await _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = userId,
                ActorName = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                ActionType = "CASE_ARCHIVED",
                EntityType = "Case",
                EntityId = entity.Id.ToString(),
                ClassificationId = archiveClassification.Id,
                CaseId = entity.Id,
                BeforeJson = JsonSerializer.Serialize(new { entity.CaseNumber, entity.Title, classificationId = sourceClassificationId }),
                AfterJson = JsonSerializer.Serialize(new { classificationId = archiveClassification.Id }),
                Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown"
            }, cancellationToken);

            return new DeleteCaseResult(entity.Id, sourceClassificationId, archiveClassification.Id);
        }

        private long ParseUserId()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            var userIdRaw =
                claimsPrincipal?.FindFirst("sub")?.Value ??
                claimsPrincipal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdRaw, out var userId) ? userId : 0;
        }
    }
}
