using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Cases.Commands.CreateCase;
using ArchiveSystem.Application.Features.Cases.Commands.DeleteCase;
using ArchiveSystem.Application.Features.Cases.Commands.MoveCase;
using ArchiveSystem.Application.Features.Cases.Commands.UpdateCase;
using ArchiveSystem.Application.Features.Cases.Commands.UpsertCaseDynamicFieldsForCurrentUser;
using ArchiveSystem.Application.Features.Cases.Models;
using ArchiveSystem.Application.Features.Cases.Queries.GetCaseByNumber;
using ArchiveSystem.Application.Features.Cases.Queries.GetCaseById;
using ArchiveSystem.Application.Features.Cases.Queries.ExportCaseWorkbook;
using ArchiveSystem.Application.Features.Cases.Queries.GetCaseDynamicFieldsForCurrentUser;
using ArchiveSystem.Application.Features.Cases.Queries.SearchCases;
using ArchiveSystem.Presentation.Api.Models;
using ArchiveSystem.Presentation.Api.Models.Cases;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchiveSystem.Presentation.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ApiController]
    [Route("api/cases")]
    public class CasesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CasesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [Authorize(Policy = "CaseReadPolicy")]
        [ProducesResponseType(typeof(ApiResult<SearchCasesResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<SearchCasesResultDto>>> ListCases(
            [FromQuery] long? classificationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string sortBy = "updatedAt",
            [FromQuery] string sortDirection = "desc",
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _mediator.Send(new SearchCasesForCurrentUserQuery
                {
                    ClassificationId = classificationId,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                }, cancellationToken);

                return Ok(ApiResult<SearchCasesResultDto>.Ok(result));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<SearchCasesResultDto>.Error(ex.ErrorCode, ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<SearchCasesResultDto>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpGet("{caseId:long}")]
        [Authorize(Policy = "CaseReadPolicy")]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResult<CaseDetailsResponse>>> GetById(long caseId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetCaseByIdQuery { CaseId = caseId }, cancellationToken);
                return Ok(ApiResult<CaseDetailsResponse>.Ok(MapToResponse(result)));
            }
            catch (NotFoundException ex)
            {
                try
                {
                    var byNumber = await _mediator.Send(new GetCaseByNumberQuery
                    {
                        CaseNumber = caseId.ToString()
                    }, cancellationToken);

                    return Ok(ApiResult<CaseDetailsResponse>.Ok(MapToResponse(byNumber)));
                }
                catch (NotFoundException)
                {
                    return Ok(ApiResult<CaseDetailsResponse>.Error("NOT_FOUND", ex.Message));
                }
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("FORBIDDEN", ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<CaseDetailsResponse>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpGet("by-number/{caseNumber}")]
        [Authorize(Policy = "CaseReadPolicy")]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResult<CaseDetailsResponse>>> GetByCaseNumber(string caseNumber, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(caseNumber) || caseNumber.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(ApiResult<CaseDetailsResponse>.Error("NOT_FOUND", "Case not found."));
                }

                var result = await _mediator.Send(new GetCaseByNumberQuery
                {
                    CaseNumber = caseNumber
                }, cancellationToken);
                return Ok(ApiResult<CaseDetailsResponse>.Ok(MapToResponse(result)));
            }
            catch (NotFoundException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("NOT_FOUND", ex.Message));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("FORBIDDEN", ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<CaseDetailsResponse>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpPost]
        [Authorize(Policy = "CaseWritePolicy")]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResult<CaseDetailsResponse>>> Create(
            [FromBody] CreateCaseRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var command = new CreateCaseCommand
                {
                    ClassificationId = request.ClassificationId,
                    ExternalCaseNumber = request.ExternalCaseNumber,
                    Address = request.Address,
                    PostalCode = request.PostalCode,
                    Owners = request.Owners.Select(owner => new CreateCaseOwnerDto
                    {
                        Id = owner.Id,
                        FullName = owner.FullName,
                        NationalCode = owner.NationalCode,
                        TaxpayerId = owner.TaxpayerId,
                        Phone = owner.Phone
                    }).ToList()
                };

                var createdCase = await _mediator.Send(command, cancellationToken);
                var payload = ApiResult<CaseDetailsResponse>.Ok(MapToResponse(createdCase));
                return CreatedAtAction(nameof(GetById), new { caseId = createdCase.Id }, payload);
            }
            catch (ValidationException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("VALIDATION_ERROR", ex.Message, ex.Errors));
            }
            catch (NotFoundException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("NOT_FOUND", ex.Message));
            }
            catch (ConflictException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error(ex.ErrorCode, ex.Message));
            }
            catch (NotAllowedExtensionFileException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<CaseDetailsResponse>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpPut("{caseId:long}")]
        [Authorize(Policy = "CaseWritePolicy")]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<CaseDetailsResponse>>> Update(
            long caseId,
            [FromBody] UpdateCaseRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new UpdateCaseCommand
                {
                    CaseId = caseId,
                    ClassificationId = request.ClassificationId,
                    ExternalCaseNumber = request.ExternalCaseNumber,
                    Address = request.Address,
                    PostalCode = request.PostalCode,
                    Owners = request.Owners.Select(owner => new UpdateCaseOwnerDto
                    {
                        Id = owner.Id,
                        FullName = owner.FullName,
                        NationalCode = owner.NationalCode,
                        TaxpayerId = owner.TaxpayerId,
                        Phone = owner.Phone
                    }).ToList()
                }, cancellationToken);

                return Ok(ApiResult<CaseDetailsResponse>.Ok(MapToResponse(result)));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.ValidationException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("VALIDATION_ERROR", ex.Message, ex.Errors));
            }
            catch (NotFoundException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("NOT_FOUND", ex.Message));
            }
            catch (ConflictException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<CaseDetailsResponse>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<CaseDetailsResponse>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpPost("{caseId:long}/update")]
        [Authorize(Policy = "CaseWritePolicy")]
        [ProducesResponseType(typeof(ApiResult<CaseDetailsResponse>), StatusCodes.Status200OK)]
        public Task<ActionResult<ApiResult<CaseDetailsResponse>>> UpdateWithPost(
            long caseId,
            [FromBody] UpdateCaseRequest request,
            CancellationToken cancellationToken)
        {
            return Update(caseId, request, cancellationToken);
        }

        [HttpPost("{caseId:long}/move")]
        [Authorize(Policy = "CaseWritePolicy")]
        [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<object>>> Move(long caseId, [FromBody] MoveCaseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new MoveCaseCommand
                {
                    CaseId = caseId,
                    SourceClassificationId = request.SourceClassificationId,
                    ClassificationId = request.ClassificationId
                }, cancellationToken);

                return Ok(ApiResult<object>.Ok(new
                {
                    moved = true,
                    caseId = result.CaseId,
                    sourceClassificationId = result.SourceClassificationId,
                    classificationId = result.TargetClassificationId
                }, "Case moved successfully."));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.ValidationException ex)
            {
                return Ok(ApiResult<object>.Error("VALIDATION_ERROR", ex.Message, ex.Errors));
            }
            catch (NotFoundException ex)
            {
                return Ok(ApiResult<object>.Error("NOT_FOUND", ex.Message));
            }
            catch (ConflictException ex)
            {
                return Ok(ApiResult<object>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<object>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<object>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<object>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpGet("{caseId:long}/export/excel")]
        [Authorize(Policy = "CaseReadPolicy")]
        [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> ExportExcel(long caseId, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ExportCaseWorkbookQuery(caseId), cancellationToken);
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpDelete("{caseId:long}")]
        [Authorize(Policy = "CaseWritePolicy")]
        [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<object>>> Delete(long caseId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new DeleteCaseCommand { CaseId = caseId }, cancellationToken);
                return Ok(ApiResult<object>.Ok(new
                {
                    archived = true,
                    caseId = result.CaseId,
                    sourceClassificationId = result.SourceClassificationId,
                    archiveClassificationId = result.ArchiveClassificationId
                }, "پرونده به بایگانی راکد منتقل شد."));
            }
            catch (NotFoundException ex)
            {
                return Ok(ApiResult<object>.Error("NOT_FOUND", ex.Message));
            }
            catch (ConflictException ex)
            {
                return Ok(ApiResult<object>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<object>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<object>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<object>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpGet("{caseId:long}/dynamic-fields")]
        [Authorize(Policy = "CaseReadPolicy")]
        [ProducesResponseType(typeof(ApiResult<CaseDynamicFieldsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<CaseDynamicFieldsDto>>> GetCaseDynamicFields(
            long caseId,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new GetCaseDynamicFieldsForCurrentUserQuery
                {
                    CaseId = caseId
                }, cancellationToken);

                return Ok(ApiResult<CaseDynamicFieldsDto>.Ok(result));
            }
            catch (NotFoundException ex)
            {
                return Ok(ApiResult<CaseDynamicFieldsDto>.Error("NOT_FOUND", ex.Message));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<CaseDynamicFieldsDto>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<CaseDynamicFieldsDto>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<CaseDynamicFieldsDto>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpPut("{caseId:long}/dynamic-fields")]
        [Authorize(Policy = "CaseWritePolicy")]
        [ProducesResponseType(typeof(ApiResult<CaseDynamicFieldsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<CaseDynamicFieldsDto>>> UpsertCaseDynamicFields(
            long caseId,
            [FromBody] UpsertCaseDynamicFieldsRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new UpsertCaseDynamicFieldsForCurrentUserCommand
                {
                    CaseId = caseId,
                    Values = request.Values.Select(MapValue).ToList()
                }, cancellationToken);

                return Ok(ApiResult<CaseDynamicFieldsDto>.Ok(result));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.ValidationException ex)
            {
                return Ok(ApiResult<CaseDynamicFieldsDto>.Error("VALIDATION_ERROR", ex.Message, ex.Errors));
            }
            catch (NotFoundException ex)
            {
                return Ok(ApiResult<CaseDynamicFieldsDto>.Error("NOT_FOUND", ex.Message));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<CaseDynamicFieldsDto>.Error(ex.ErrorCode, ex.Message));
            }
            catch (ForbiddenAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ApiResult<CaseDynamicFieldsDto>.Error("FORBIDDEN", ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<CaseDynamicFieldsDto>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        [HttpPost("search")]
        [Authorize(Policy = "CaseReadPolicy")]
        [ProducesResponseType(typeof(ApiResult<SearchCasesResultDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResult<SearchCasesResultDto>>> SearchCases(
            [FromBody] SearchCasesRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _mediator.Send(new SearchCasesForCurrentUserQuery
                {
                    ClassificationId = request.ClassificationId,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    SortBy = request.SortBy,
                    SortDirection = request.SortDirection,
                    OwnerName = request.OwnerName,
                    ExternalCaseNumber = request.ExternalCaseNumber,
                    CreatedBy = request.CreatedBy,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    Tags = request.Tags,
                    DynamicFilterLogic = request.DynamicFilterExpression?.Logic ?? "and",
                    DynamicFilterGroups = request.DynamicFilterExpression?.Groups
                        .Select(group => new DynamicFilterGroupDto
                        {
                            Logic = group.Logic,
                            Filters = group.Filters.Select(MapDynamicFilter).ToList()
                        })
                        .ToList() ?? new List<DynamicFilterGroupDto>(),
                    DynamicFilters = request.DynamicFilters.Select(filter => new DynamicFilterDto
                        {
                            FieldId = filter.FieldId,
                            Op = filter.Op,
                            Value = filter.Value,
                            FromValue = filter.FromValue,
                            ToValue = filter.ToValue,
                            Values = filter.Values ?? new List<System.Text.Json.JsonElement>()
                        }).ToList()
                }, cancellationToken);

                return Ok(ApiResult<SearchCasesResultDto>.Ok(result));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.ValidationException ex)
            {
                return Ok(ApiResult<SearchCasesResultDto>.Error("VALIDATION_ERROR", ex.Message, ex.Errors));
            }
            catch (ArchiveSystem.Application.Common.Exceptions.UnauthorizedAccessException ex)
            {
                return Ok(ApiResult<SearchCasesResultDto>.Error(ex.ErrorCode, ex.Message));
            }
            catch (Exception ex)
            {
                return Ok(ApiResult<SearchCasesResultDto>.Error("INTERNAL_SERVER_ERROR", ex.Message));
            }
        }

        private static DynamicFilterDto MapDynamicFilter(DynamicFilterRequest filter)
        {
            return new DynamicFilterDto
            {
                FieldId = filter.FieldId,
                Op = filter.Op,
                Value = filter.Value,
                FromValue = filter.FromValue,
                ToValue = filter.ToValue,
                Values = filter.Values ?? new List<System.Text.Json.JsonElement>()
            };
        }

        private static CaseDetailsResponse MapToResponse(CaseDetailsDto dto)
        {
            return new CaseDetailsResponse
            {
                Id = dto.Id,
                CaseNumber = dto.CaseNumber,
                ExternalCaseNumber = dto.ExternalCaseNumber,
                AvatarImagePath = dto.AvatarImagePath,
                Address = dto.Address,
                PostalCode = dto.PostalCode,
                UpdatedAt = dto.UpdatedAt,
                ClassificationId = dto.ClassificationId,
                ClassificationTitle = dto.ClassificationTitle,
                OwnerNames = dto.OwnerNames,
                Tags = dto.Tags,
                Owners = dto.Owners.Select(owner => new CaseOwnerResponse
                {
                    Id = owner.Id,
                    FullName = owner.FullName,
                    NationalCode = owner.NationalCode,
                    TaxpayerId = owner.TaxpayerId,
                    Phone = owner.Phone
                }).ToList(),
                History = dto.History.Select(item => new CaseHistoryItemResponse
                {
                    Id = item.Id,
                    Timestamp = item.Timestamp,
                    ActorName = item.ActorName,
                    ActionType = item.ActionType,
                    EntityType = item.EntityType,
                    Detail = item.Detail
                }).ToList()
            };
        }

        private static CaseDynamicFieldValueDto MapValue(CaseDynamicFieldValueRequest request)
        {
            return new CaseDynamicFieldValueDto
            {
                FieldId = request.FieldId,
                StringValue = request.StringValue,
                NumberValue = request.NumberValue,
                DateValue = request.DateValue,
                BoolValue = request.BoolValue,
                OptionIds = request.OptionIds ?? new List<Guid>()
            };
        }
    }
}
