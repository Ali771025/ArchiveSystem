using System.Text.Json;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Cases.Models;
using ArchiveSystem.Application.Features.Cases.Queries.GetCaseById;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Cases.Commands.UpdateCase
{
    public sealed class UpdateCaseCommandHandler : IRequestHandler<UpdateCaseCommand, CaseDetailsDto>
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IAuditWriter _auditWriter;
        private readonly IMediator _mediator;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public UpdateCaseCommandHandler(
            ICaseRepository caseRepository,
            IAccessControlService accessControlService,
            IAuditWriter auditWriter,
            IMediator mediator,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _caseRepository = caseRepository;
            _accessControlService = accessControlService;
            _auditWriter = auditWriter;
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CaseDetailsDto> Handle(UpdateCaseCommand request, CancellationToken cancellationToken)
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
                    "You are not allowed to update this case.");
            }

            if (request.ClassificationId.HasValue && request.ClassificationId.Value != entity.ClassificationId)
            {
                throw new ValidationException("Use the move endpoint to change case classification.");
            }

            var normalizedExternalCaseNumber = NormalizeOptional(request.ExternalCaseNumber);
            if (!string.Equals(normalizedExternalCaseNumber, entity.ExternalCaseNumber, StringComparison.OrdinalIgnoreCase)
                && normalizedExternalCaseNumber != null)
            {
                var existing = await _caseRepository.GetByExternalCaseNumberAsync(normalizedExternalCaseNumber, cancellationToken);
                if (existing != null && existing.Id != entity.Id)
                {
                    throw new ConflictException($"شماره مرجع «{normalizedExternalCaseNumber}» قبلاً ثبت شده است.");
                }
            }

            var normalizedOwners = request.Owners.Select((owner, index) => new CaseOwnerDto
            {
                Id = owner.Id <= 0 ? index + 1 : owner.Id,
                FullName = owner.FullName.Trim(),
                NationalCode = owner.NationalCode.Trim(),
                TaxpayerId = NormalizeOptional(owner.TaxpayerId),
                Phone = owner.Phone.Trim()
            }).ToList();

            var before = new
            {
                entity.CaseNumber,
                entity.ExternalCaseNumber,
                entity.Title,
                entity.Location,
                entity.PostalCode,
                entity.Owners
            };

            entity.UpdateExternalCaseNumber(normalizedExternalCaseNumber);
            entity.UpdateTitle(request.Address.Trim());
            entity.UpdateLocation(request.Address.Trim());
            entity.UpdatePostalCode(NormalizeOptional(request.PostalCode));
            entity.UpdateOwners(JsonSerializer.Serialize(normalizedOwners));

            await _caseRepository.UpdateAsync(entity, cancellationToken);
            await _caseRepository.SaveChangesAsync(cancellationToken);

            await _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = userId,
                ActorName = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                ActionType = "CASE_UPDATED",
                EntityType = "Case",
                EntityId = entity.Id.ToString(),
                ClassificationId = entity.ClassificationId,
                CaseId = entity.Id,
                BeforeJson = JsonSerializer.Serialize(before),
                AfterJson = JsonSerializer.Serialize(new
                {
                    entity.CaseNumber,
                    entity.ExternalCaseNumber,
                    entity.Title,
                    entity.Location,
                    entity.PostalCode,
                    entity.Owners
                }),
                Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown"
            }, cancellationToken);

            return await _mediator.Send(new GetCaseByIdQuery { CaseId = entity.Id }, cancellationToken);
        }

        private long ParseUserId()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            var userIdRaw =
                claimsPrincipal?.FindFirst("sub")?.Value ??
                claimsPrincipal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdRaw, out var userId) ? userId : 0;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
