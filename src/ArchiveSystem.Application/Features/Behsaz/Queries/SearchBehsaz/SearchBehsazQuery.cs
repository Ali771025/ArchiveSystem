using ArchiveSystem.Application.Common.Interfaces.Security;
using ArchiveSystem.Domain.Common;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Integrations;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.Behsaz.Queries.SearchBehsaz
{
    public sealed class SearchBehsazQuery : IRequest<PagedResult<BehsazSearchResultDto>>
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public int InstaurationFilterType { get; init; } = 1;
        public int? Area { get; init; }
        public int? Zone { get; init; }
        public int? Block { get; init; }
        public int? Land { get; init; }
        public int? Building { get; init; }
        public int? Apartment { get; init; }
        public int? Business { get; init; }
        public string? SystemNumber { get; init; }
        public string? FileNumber { get; init; }
        public decimal? GhateCode { get; init; }
        public decimal? RegistrationPlateMain { get; init; }
        public decimal? RegistrationPlateSub { get; init; }
        public string? Address { get; init; }
        public int? PropertyStatus { get; init; }
        public bool? GisIndicated { get; init; }
        public TaxpayerGroup? TaxpayerGroup { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? NationalCode { get; init; }
        public string? CompanyName { get; init; }
        public string? NationalCompanyId { get; init; }
        public string? RegisterNumber { get; init; }
        public int? UserCode { get; init; }
        public long? CategoryId { get; init; }
    }

    public sealed class SearchBehsazQueryHandler : IRequestHandler<SearchBehsazQuery, PagedResult<BehsazSearchResultDto>>
    {
        private readonly IBehsazRepository _repository;
        private readonly ICurrentUserContext _currentUserContext;
        private readonly ArchiveSystem.Application.Common.Interfaces.Security.IAccessControlService _accessControlService;

        public SearchBehsazQueryHandler(
            IBehsazRepository repository,
            ICurrentUserContext currentUserContext,
            ArchiveSystem.Application.Common.Interfaces.Security.IAccessControlService accessControlService)
        {
            _repository = repository;
            _currentUserContext = currentUserContext;
            _accessControlService = accessControlService;
        }

        public async Task<PagedResult<BehsazSearchResultDto>> Handle(
            SearchBehsazQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserContext.UserId;
            if (!_currentUserContext.IsAuthenticated || userId is null)
            {
                throw new UnauthorizedAccessException("Authenticated user context is required for Behsaz search.");
            }

            if (request.CategoryId.HasValue &&
                !await CanReadCategoryAsync(userId.Value, request.CategoryId.Value, cancellationToken))
            {
                throw new UnauthorizedAccessException("User is not allowed to search Behsaz data for the selected category.");
            }

            return await _repository.SearchAsync(new BehsazSearchCriteria
            {
                Page = Math.Max(1, request.Page),
                PageSize = Math.Clamp(request.PageSize, 1, 200),
                InstaurationFilterType = request.InstaurationFilterType <= 0 ? 1 : request.InstaurationFilterType,
                Area = request.Area,
                Zone = request.Zone,
                Block = request.Block,
                Land = request.Land,
                Building = request.Building,
                Apartment = request.Apartment,
                Business = request.Business,
                SystemNumber = Normalize(request.SystemNumber),
                FileNumber = Normalize(request.FileNumber),
                GhateCode = request.GhateCode,
                RegistrationPlateMain = request.RegistrationPlateMain,
                RegistrationPlateSub = request.RegistrationPlateSub,
                Address = Normalize(request.Address),
                PropertyStatus = request.PropertyStatus,
                GisIndicated = request.GisIndicated,
                TaxpayerGroup = request.TaxpayerGroup,
                FirstName = Normalize(request.FirstName),
                LastName = Normalize(request.LastName),
                NationalCode = Normalize(request.NationalCode),
                CompanyName = Normalize(request.CompanyName),
                NationalCompanyId = Normalize(request.NationalCompanyId),
                RegisterNumber = Normalize(request.RegisterNumber),
                UserCode = request.UserCode.HasValue ? Math.Max(1, request.UserCode.Value) : null
            }, cancellationToken);
        }

        private async Task<bool> CanReadCategoryAsync(
            long userId,
            long categoryId,
            CancellationToken cancellationToken)
        {
            if (_currentUserContext.IsInRole("Admin") ||
                _currentUserContext.IsInRole("Superuser") ||
                await _accessControlService.HasGlobalPermissionAsync(userId, "admin.access", cancellationToken))
            {
                return true;
            }

            return await _accessControlService.CanReadClassificationAsync(userId, categoryId, cancellationToken);
        }

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
