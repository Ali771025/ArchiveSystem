using System.Security.Claims;
using System.Text.Json;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Classifications.Validation;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Cases.Commands.MoveCase
{
    public sealed class MoveCaseCommandHandler : IRequestHandler<MoveCaseCommand, MoveCaseResult>
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IClassificationRepository _classificationRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IAuditWriter _auditWriter;
        private readonly IValidator<AssignDossierToClassificationRule> _assignDossierToClassificationValidator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MoveCaseCommandHandler(
            ICaseRepository caseRepository,
            IClassificationRepository classificationRepository,
            IAccessControlService accessControlService,
            IAuditWriter auditWriter,
            IValidator<AssignDossierToClassificationRule> assignDossierToClassificationValidator,
            IHttpContextAccessor httpContextAccessor)
        {
            _caseRepository = caseRepository;
            _classificationRepository = classificationRepository;
            _accessControlService = accessControlService;
            _auditWriter = auditWriter;
            _assignDossierToClassificationValidator = assignDossierToClassificationValidator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<MoveCaseResult> Handle(MoveCaseCommand request, CancellationToken cancellationToken)
        {
            var entity = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken);
            if (entity == null)
            {
                throw new NotFoundException($"Case '{request.CaseId}' not found.");
            }

            var sourceClassificationId = entity.ClassificationId;
            var userId = GetUserId();
            var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true ||
                          await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);

            if (request.SourceClassificationId.HasValue &&
                request.SourceClassificationId.Value != sourceClassificationId)
            {
                throw new ConflictException(
                    $"Case '{request.CaseId}' is no longer in classification '{request.SourceClassificationId.Value}'.");
            }

            if (!isAdmin && !await _accessControlService.CanMoveClassificationAsync(userId, sourceClassificationId, cancellationToken))
            {
                throw new ForbiddenAccessException(
                    "You are not allowed to move this case from source classification.");
            }

            var target = await _classificationRepository.GetByIdAsync(request.ClassificationId, cancellationToken);
            if (target == null || !target.IsActive)
            {
                throw new NotFoundException($"Classification '{request.ClassificationId}' not found.");
            }

            var leafValidation = await _assignDossierToClassificationValidator.ValidateAsync(
                new AssignDossierToClassificationRule(request.ClassificationId),
                cancellationToken);
            if (!leafValidation.IsValid)
            {
                throw new ArchiveSystem.Application.Common.Exceptions.ValidationException(leafValidation.Errors);
            }

            if (!isAdmin && !await _accessControlService.CanMoveClassificationAsync(userId, request.ClassificationId, cancellationToken))
            {
                throw new ForbiddenAccessException(
                    "You are not allowed to move this case to target classification.");
            }

            if (sourceClassificationId == request.ClassificationId)
            {
                return new MoveCaseResult(entity.Id, sourceClassificationId, request.ClassificationId);
            }

            var classifications = await _classificationRepository.GetAllAsync(cancellationToken);
            var classificationMap = classifications.ToDictionary(item => item.Id);
            var sourceClassificationPath = BuildClassificationTitlePath(sourceClassificationId, classificationMap);
            var targetClassificationPath = BuildClassificationTitlePath(request.ClassificationId, classificationMap);

            var moved = await _caseRepository.MoveToClassificationAsync(
                request.CaseId,
                request.ClassificationId,
                cancellationToken);
            if (!moved)
            {
                throw new ConflictException(
                    $"Case '{request.CaseId}' could not be moved.");
            }

            var persisted = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken);
            if (persisted?.ClassificationId != request.ClassificationId)
            {
                throw new ConflictException(
                    $"Case '{request.CaseId}' move was not persisted to classification '{request.ClassificationId}'.");
            }

            await _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = userId,
                ActorName = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                ActionType = "CASE_MOVED",
                EntityType = "Case",
                EntityId = entity.Id.ToString(),
                ClassificationId = request.ClassificationId,
                CaseId = entity.Id,
                BeforeJson = JsonSerializer.Serialize(new
                {
                    classificationId = sourceClassificationId,
                    classificationPath = sourceClassificationPath
                }),
                AfterJson = JsonSerializer.Serialize(new
                {
                    classificationId = request.ClassificationId,
                    classificationPath = targetClassificationPath
                }),
                Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown"
            }, cancellationToken);

            return new MoveCaseResult(entity.Id, sourceClassificationId, request.ClassificationId);
        }

        private static string BuildClassificationTitlePath(
            long classificationId,
            IReadOnlyDictionary<long, ArchiveSystem.Domain.Entities.Classification> classifications)
        {
            var segments = new List<string>();
            var visited = new HashSet<long>();
            var currentId = classificationId;

            while (visited.Add(currentId) && classifications.TryGetValue(currentId, out var classification))
            {
                segments.Add(classification.Title);
                if (!classification.ParentId.HasValue)
                {
                    break;
                }

                currentId = classification.ParentId.Value;
            }

            segments.Reverse();
            return segments.Count > 0
                ? string.Join("/", segments)
                : classificationId.ToString();
        }

        private long GetUserId()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            var userIdRaw =
                claimsPrincipal?.FindFirst("sub")?.Value ??
                claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdRaw, out var userId) ? userId : 0;
        }
    }
}
