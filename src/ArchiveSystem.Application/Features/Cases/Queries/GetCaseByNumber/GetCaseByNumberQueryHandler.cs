using System.Security.Claims;
using System.Text.Json;
using ArchiveSystem.Application.AccessControl;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Features.Cases.Models;
using ArchiveSystem.Application.Features.Reports.Interfaces;
using ArchiveSystem.Application.Features.Reports.Models;
using ArchiveSystem.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace ArchiveSystem.Application.Features.Cases.Queries.GetCaseByNumber
{
    public sealed class GetCaseByNumberQueryHandler : IRequestHandler<GetCaseByNumberQuery, CaseDetailsDto>
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IClassificationRepository _classificationRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IMovementLogRepository _movementLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetCaseByNumberQueryHandler(
            ICaseRepository caseRepository,
            IClassificationRepository classificationRepository,
            IAccessControlService accessControlService,
            IMovementLogRepository movementLogRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _caseRepository = caseRepository;
            _classificationRepository = classificationRepository;
            _accessControlService = accessControlService;
            _movementLogRepository = movementLogRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CaseDetailsDto> Handle(GetCaseByNumberQuery request, CancellationToken cancellationToken)
        {
            var caseNumber = request.CaseNumber?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(caseNumber))
            {
                throw new NotFoundException("Case number is required.");
            }

            var caseEntity = await _caseRepository.GetByCaseNumberFlexibleAsync(caseNumber, cancellationToken);
            if (caseEntity == null && long.TryParse(caseNumber, out var caseId))
            {
                caseEntity = await _caseRepository.GetByIdAsync(caseId, cancellationToken);
            }

            if (caseEntity == null)
            {
                throw new NotFoundException($"Case '{caseNumber}' not found.");
            }

            var userId = GetUserId();
            var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true ||
                          await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);

            if (!isAdmin)
            {
                var canRead = await _accessControlService.CanReadClassificationAsync(userId, caseEntity.ClassificationId, cancellationToken);
                if (!canRead)
                {
                    throw new ForbiddenAccessException(
                        "You are not allowed to view this case.");
                }
            }

            var classification = await _classificationRepository.GetByIdAsync(caseEntity.ClassificationId, cancellationToken);
            var history = await GetCaseHistoryAsync(caseEntity.Id, cancellationToken);
            var owners = ParseOwners(caseEntity.Owners);

            return new CaseDetailsDto
            {
                Id = caseEntity.Id,
                CaseNumber = caseEntity.CaseNumber,
                ExternalCaseNumber = caseEntity.ExternalCaseNumber,
                AvatarImagePath = caseEntity.AvatarImagePath,
                Address = caseEntity.Location,
                PostalCode = caseEntity.PostalCode,
                UpdatedAt = caseEntity.ModifiedAt ?? caseEntity.CreatedAt,
                ClassificationId = caseEntity.ClassificationId,
                ClassificationTitle = classification?.Title ?? string.Empty,
                OwnerNames = owners.Select(x => x.FullName).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
                Tags = new List<string>(),
                Owners = owners.ToList(),
                History = history.ToList()
            };
        }

        private async Task<IReadOnlyList<CaseHistoryItemDto>> GetCaseHistoryAsync(long caseId, CancellationToken cancellationToken)
        {
            var context = await BuildActivityContextAsync(cancellationToken);
            var result = await _movementLogRepository.QueryActivityAsync(
                new ActivityReportFilter
                {
                    CaseId = caseId,
                    Page = 1,
                    PageSize = 50,
                    SortBy = "timestamp",
                    SortDescending = true
                },
                context,
                cancellationToken);

            return result.Items.Select(x => new CaseHistoryItemDto
            {
                Id = x.Id,
                Timestamp = x.Timestamp,
                ActorName = x.ActorName ?? "System",
                ActionType = x.ActionType,
                EntityType = x.EntityType,
                Detail = CaseHistoryDetailFormatter.Format(x)
            }).ToList();
        }

        private async Task<ActivityReportContext> BuildActivityContextAsync(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            var isAdmin = _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true ||
                          await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken);
            if (isAdmin)
            {
                return new ActivityReportContext { IsAdmin = true };
            }

            var classifications = await _accessControlService.GetAccessibleClassificationIdsAsync(
                userId,
                Domain.Enums.ClassificationPermissionMask.Read,
                cancellationToken);
            var tags = await _accessControlService.GetAllowedTagIdsAsync(
                userId,
                Domain.Enums.TagPermissionMask.View,
                cancellationToken);

            return new ActivityReportContext
            {
                IsAdmin = false,
                AllowedClassificationIds = classifications,
                AllowedTagIds = tags
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

        private static IReadOnlyList<CaseOwnerDto> ParseOwners(string ownersJson)
        {
            if (string.IsNullOrWhiteSpace(ownersJson))
            {
                return Array.Empty<CaseOwnerDto>();
            }

            try
            {
                var owners = JsonSerializer.Deserialize<List<CaseOwnerDto>>(ownersJson);
                if (owners == null)
                {
                    return Array.Empty<CaseOwnerDto>();
                }

                return owners
                    .Select((owner, index) => new CaseOwnerDto
                    {
                        Id = owner.Id == 0 ? index + 1 : owner.Id,
                        FullName = owner.FullName?.Trim() ?? string.Empty,
                        NationalCode = owner.NationalCode?.Trim() ?? string.Empty,
                        TaxpayerId = string.IsNullOrWhiteSpace(owner.TaxpayerId) ? null : owner.TaxpayerId.Trim(),
                        Phone = owner.Phone?.Trim() ?? string.Empty
                    })
                    .ToList();
            }
            catch
            {
                return Array.Empty<CaseOwnerDto>();
            }
        }
    }
}
