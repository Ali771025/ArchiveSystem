using ArchiveSystem.Application.Common.Models;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Features.Behsaz.Queries.GetBehsazRecords;
using ArchiveSystem.Application.Features.Behsaz.Queries.SearchBehsaz;
using ArchiveSystem.Domain.Common;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Integrations;
using ArchiveSystem.Presentation.Api.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [RequirePermission(Permissions.Behsaz.Search)]
    [ApiController]
    [Route("api/behsaz")]
    public sealed class BehsazIntegrationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BehsazIntegrationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("records")]
        [ProducesResponseType(typeof(PaginatedResult<BehsazRecordViewModel>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedResult<BehsazRecordViewModel>>> GetRecords(
            [FromQuery] int? tInfBuildingId,
            [FromQuery] int? tBasPeopleGroupId,
            [FromQuery] int? instaurationArea,
            [FromQuery] int? instaurationZone,
            [FromQuery] int? instaurationBlock,
            [FromQuery] int? instaurationLand,
            [FromQuery] int instaurationFilterType,
            [FromQuery] int? instaurationBuilding,
            [FromQuery] int? instaurationApartment,
            [FromQuery] int? instaurationBusiness,
            [FromQuery] string? manualNumber,
            [FromQuery] string? number,
            [FromQuery] decimal? ghateCode,
            [FromQuery] int pageSize = 20,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int? userCode = null,
            CancellationToken cancellationToken = default)
        {
            var query = new GetBehsazRecordsQuery
            {
                TINFBuildingID = tInfBuildingId,
                TBASPeopleGroupID = tBasPeopleGroupId,
                InstaurationArea = instaurationArea,
                InstaurationZone = instaurationZone,
                InstaurationBlock = instaurationBlock,
                InstaurationLand = instaurationLand,
                InstaurationFilterType = instaurationFilterType <= 0 ? 1 : instaurationFilterType,
                InstaurationBuilding = instaurationBuilding,
                InstaurationApartment = instaurationApartment,
                InstaurationBusiness = instaurationBusiness,
                ManualNumber = manualNumber,
                Number = number,
                GhateCode = ghateCode,
                PageSize = Math.Clamp(pageSize, 1, 200),
                PageIndex = Math.Max(0, pageIndex),
                UserCode = userCode.HasValue ? Math.Max(1, userCode.Value) : null,
                SearchValues = Array.Empty<BehsazSearchValue>()
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(PagedResult<BehsazSearchResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<BehsazSearchResultDto>>> Search(
            [FromBody] SearchBehsazRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new SearchBehsazQuery
            {
                Page = request.Page ?? Math.Max(1, request.PageIndex + 1),
                PageSize = Math.Clamp(request.PageSize, 1, 200),
                Area = request.Area ?? request.InstaurationArea,
                Zone = request.Zone ?? request.InstaurationZone,
                Block = request.Block ?? request.InstaurationBlock,
                Land = request.Land ?? request.InstaurationLand,
                InstaurationFilterType = request.InstaurationFilterType <= 0 ? 1 : request.InstaurationFilterType,
                Building = request.Building ?? request.InstaurationBuilding,
                Apartment = request.Apartment ?? request.InstaurationApartment,
                Business = request.Business ?? request.InstaurationBusiness,
                SystemNumber = request.SystemNumber,
                FileNumber = request.FileNumber,
                GhateCode = request.GhateCode ?? request.Ghate,
                RegistrationPlateMain = request.RegistrationPlateMain ?? request.MainPlate,
                RegistrationPlateSub = request.RegistrationPlateSub ?? request.SubPlate,
                Address = request.Address,
                PropertyStatus = request.PropertyStatus,
                GisIndicated = request.GisIndicated,
                TaxpayerGroup = request.TaxpayerGroup.HasValue ? (TaxpayerGroup)request.TaxpayerGroup.Value : null,
                FirstName = request.FirstName,
                LastName = request.LastName,
                NationalCode = request.NationalCode,
                CompanyName = request.CompanyName,
                NationalCompanyId = request.NationalCompanyId,
                UserCode = request.UserCode.HasValue ? Math.Max(1, request.UserCode.Value) : null,
                CategoryId = request.CategoryId
            };

            return Ok(await _mediator.Send(query, cancellationToken));
        }
    }

    public sealed class SearchBehsazRequest
    {
        public int? Page { get; init; }
        public int PageSize { get; init; } = 20;
        public int PageIndex { get; init; }
        public int? Area { get; init; }
        public int? Zone { get; init; }
        public int? Block { get; init; }
        public int? Land { get; init; }
        public int InstaurationFilterType { get; init; } = 1;
        public int? Building { get; init; }
        public int? Apartment { get; init; }
        public int? Business { get; init; }
        public int? InstaurationArea { get; init; }
        public int? InstaurationZone { get; init; }
        public int? InstaurationBlock { get; init; }
        public int? InstaurationLand { get; init; }
        public int? InstaurationBuilding { get; init; }
        public int? InstaurationApartment { get; init; }
        public int? InstaurationBusiness { get; init; }
        public string? SystemNumber { get; init; }
        public string? FileNumber { get; init; }
        public decimal? GhateCode { get; init; }
        public decimal? Ghate { get; init; }
        public decimal? RegistrationPlateMain { get; init; }
        public decimal? RegistrationPlateSub { get; init; }
        public decimal? MainPlate { get; init; }
        public decimal? SubPlate { get; init; }
        public string? Address { get; init; }
        public int? PropertyStatus { get; init; }
        public bool? GisIndicated { get; init; }
        public int? TaxpayerGroup { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? NationalCode { get; init; }
        public string? CompanyName { get; init; }
        public string? NationalCompanyId { get; init; }
        public int? UserCode { get; init; }
        public long? CategoryId { get; init; }
    }
}
