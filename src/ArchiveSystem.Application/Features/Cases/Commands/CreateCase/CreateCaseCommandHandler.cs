using System.Security.Claims;
using System.Text.Json;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Features.Cases.Models;
using ArchiveSystem.Application.Features.Cases.Interfaces;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Entities;
using ArchiveSystem.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Cases.Commands.CreateCase
{
    public sealed class CreateCaseCommandHandler : IRequestHandler<CreateCaseCommand, CaseDetailsDto>
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IClassificationRepository _classificationRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IAuditWriter _auditWriter;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICaseNumberGenerator _caseNumberGenerator;

        public CreateCaseCommandHandler(
            ICaseRepository caseRepository,
            IClassificationRepository classificationRepository,
            IAccessControlService accessControlService,
            IAuditWriter auditWriter,
            IHttpContextAccessor httpContextAccessor,
            ICaseNumberGenerator caseNumberGenerator)
        {
            _caseRepository = caseRepository;
            _classificationRepository = classificationRepository;
            _accessControlService = accessControlService;
            _auditWriter = auditWriter;
            _httpContextAccessor = httpContextAccessor;
            _caseNumberGenerator = caseNumberGenerator;
        }

        public async Task<CaseDetailsDto> Handle(CreateCaseCommand request, CancellationToken cancellationToken)
        {
            var classification = await _classificationRepository.GetByIdAsync(request.ClassificationId, cancellationToken);
            if (classification == null || !classification.IsActive)
            {
                throw new NotFoundException($"Classification '{request.ClassificationId}' not found.");
            }

            var userId = GetUserId();
            var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true ||
                          await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);

            if (!isAdmin)
            {
                var canWrite = await _accessControlService.CanWriteClassificationAsync(userId, request.ClassificationId, cancellationToken);
                if (!canWrite)
                {
                    throw new ForbiddenAccessException(
                        "You are not allowed to create case in this classification.");
                }
            }

            if (await _classificationRepository.HasChildrenAsync(request.ClassificationId, cancellationToken))
            {
                throw new ValidationException("Cases can only be created in leaf classifications.");
            }

            var normalizedExternalCaseNumber = NormalizeOptional(request.ExternalCaseNumber);
            if (normalizedExternalCaseNumber != null)
            {
                var existingCase = await _caseRepository.GetByExternalCaseNumberAsync(normalizedExternalCaseNumber, cancellationToken);
                if (existingCase != null)
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

            var generatedCaseNumber = await _caseNumberGenerator.GenerateAsync(cancellationToken);
            var entity = new Case(
                id: 0,
                caseNumber: generatedCaseNumber,
                title: request.Address.Trim(),
                classificationId: request.ClassificationId,
                owners: JsonSerializer.Serialize(normalizedOwners),
                location: request.Address.Trim(),
                postalCode: NormalizeOptional(request.PostalCode),
                externalCaseNumber: normalizedExternalCaseNumber);
            entity.SetCreatedBy(userId, _httpContextAccessor.HttpContext?.User?.Identity?.Name);

            await _caseRepository.AddAsync(entity, cancellationToken);
            await _caseRepository.SaveChangesAsync(cancellationToken);

            await _auditWriter.WriteAsync(new MovementLogWriteModel
            {
                ActorUserId = userId,
                ActorName = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                ActionType = "CASE_CREATED",
                EntityType = "Case",
                EntityId = entity.Id.ToString(),
                ClassificationId = entity.ClassificationId,
                CaseId = entity.Id,
                AfterJson = JsonSerializer.Serialize(new
                {
                    entity.CaseNumber,
                    entity.ExternalCaseNumber,
                    entity.Title,
                    entity.Location,
                    entity.PostalCode
                }),
                Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                UserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown"
            }, cancellationToken);

            return new CaseDetailsDto
            {
                Id = entity.Id,
                CaseNumber = entity.CaseNumber,
                ExternalCaseNumber = entity.ExternalCaseNumber,
                AvatarImagePath = entity.AvatarImagePath,
                Address = entity.Location,
                PostalCode = entity.PostalCode,
                UpdatedAt = entity.ModifiedAt ?? entity.CreatedAt,
                ClassificationId = entity.ClassificationId,
                ClassificationTitle = classification.Title,
                OwnerNames = normalizedOwners.Select(x => x.FullName).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
                Tags = new List<string>(),
                Owners = normalizedOwners,
                History = new List<CaseHistoryItemDto>()
            };
        }

        private long GetUserId()
        {
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            var userIdRaw =
                claimsPrincipal?.FindFirst("sub")?.Value ??
                claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdRaw, out var userId) ? userId : 0;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
