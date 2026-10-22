using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Repositories;
using MediatR;

namespace ArchiveSystem.Application.Features.DynamicFields.Queries.GetCaseDynamicFields
{
    public sealed class GetCaseDynamicFieldsQuery : IRequest<CaseDynamicFieldsDto>
    {
        public GetCaseDynamicFieldsQuery(long caseId)
        {
            CaseId = caseId;
        }

        public long CaseId { get; }
    }

    public sealed class GetCaseDynamicFieldsQueryHandler : IRequestHandler<GetCaseDynamicFieldsQuery, CaseDynamicFieldsDto>
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IDynamicTemplateRepository _dynamicTemplateRepository;
        private readonly ICaseDynamicFieldValueRepository _caseDynamicFieldValueRepository;

        public GetCaseDynamicFieldsQueryHandler(
            ICaseRepository caseRepository,
            IDynamicTemplateRepository dynamicTemplateRepository,
            ICaseDynamicFieldValueRepository caseDynamicFieldValueRepository)
        {
            _caseRepository = caseRepository;
            _dynamicTemplateRepository = dynamicTemplateRepository;
            _caseDynamicFieldValueRepository = caseDynamicFieldValueRepository;
        }

        public async Task<CaseDynamicFieldsDto> Handle(GetCaseDynamicFieldsQuery request, CancellationToken cancellationToken)
        {
            var caseEntity = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken);
            if (caseEntity == null)
            {
                throw new NotFoundException($"Case '{request.CaseId}' was not found.");
            }

            var templates = await _dynamicTemplateRepository.GetActiveHierarchyByClassificationIdAsync(
                caseEntity.ClassificationId,
                cancellationToken);
            if (templates.Count == 0)
            {
                return new CaseDynamicFieldsDto
                {
                    CaseId = caseEntity.Id,
                    ClassificationId = caseEntity.ClassificationId,
                    TemplateId = Guid.Empty
                };
            }

            var storedValues = await _caseDynamicFieldValueRepository.GetCaseValuesByCaseIdAsync(request.CaseId, cancellationToken);

            var fieldList = templates
                .SelectMany(template => template.Groups
                    .OrderBy(group => group.Order)
                    .SelectMany(group => group.Fields.OrderBy(field => field.Order)))
                .Where(f => f.IsActive)
                .ToList();

            var values = new List<CaseDynamicFieldValueDto>();
            foreach (var field in fieldList)
            {
                if (storedValues.TryGetValue(field.Id, out var storedValue))
                {
                    storedValue.Key = field.Key;
                    storedValue.Label = field.Label;
                    storedValue.FieldType = field.FieldType;
                    storedValue.IsRequired = field.IsRequired;
                    storedValue.IsMultiSelect = field.IsMultiSelect;
                    storedValue.Options = field.Options
                        .Where(o => o.IsActive)
                        .OrderBy(o => o.Order)
                        .ToList();
                    values.Add(storedValue);
                }
                else
                {
                    values.Add(new CaseDynamicFieldValueDto
                    {
                        FieldId = field.Id,
                        Key = field.Key,
                        Label = field.Label,
                        FieldType = field.FieldType,
                        IsRequired = field.IsRequired,
                        IsMultiSelect = field.IsMultiSelect,
                        Options = field.Options
                            .Where(o => o.IsActive)
                            .OrderBy(o => o.Order)
                            .ToList()
                    });
                }
            }

            return new CaseDynamicFieldsDto
            {
                CaseId = caseEntity.Id,
                ClassificationId = caseEntity.ClassificationId,
                TemplateId = templates[^1].Id,
                Values = values
            };
        }
    }
}
