using System.Text.RegularExpressions;
using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.DynamicFields.Queries.GetCaseDynamicFields;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Enums;
using ArchiveSystem.Domain.Repositories;
using FluentValidation.Results;
using MediatR;

namespace ArchiveSystem.Application.Features.DynamicFields.Commands.UpsertCaseDynamicFields
{
    public sealed class UpsertCaseDynamicFieldsCommand : IRequest<CaseDynamicFieldsDto>
    {
        public long CaseId { get; set; }
        public List<CaseDynamicFieldValueDto> Values { get; set; } = new();
    }

    public sealed class UpsertCaseDynamicFieldsCommandHandler : IRequestHandler<UpsertCaseDynamicFieldsCommand, CaseDynamicFieldsDto>
    {
        private readonly ICaseRepository _caseRepository;
        private readonly IDynamicTemplateRepository _dynamicTemplateRepository;
        private readonly ICaseDynamicFieldValueRepository _caseDynamicFieldValueRepository;
        private readonly IMediator _mediator;

        public UpsertCaseDynamicFieldsCommandHandler(
            ICaseRepository caseRepository,
            IDynamicTemplateRepository dynamicTemplateRepository,
            ICaseDynamicFieldValueRepository caseDynamicFieldValueRepository,
            IMediator mediator)
        {
            _caseRepository = caseRepository;
            _dynamicTemplateRepository = dynamicTemplateRepository;
            _caseDynamicFieldValueRepository = caseDynamicFieldValueRepository;
            _mediator = mediator;
        }

        public async Task<CaseDynamicFieldsDto> Handle(UpsertCaseDynamicFieldsCommand request, CancellationToken cancellationToken)
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
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.CaseId), "No active dynamic template was found for this case classification hierarchy.")
                });
            }

            var activeFields = templates
                .SelectMany(template => template.Groups)
                .SelectMany(group => group.Fields)
                .Where(f => f.IsActive)
                .ToDictionary(f => f.Id);

            var failures = new List<ValidationFailure>();
            var incomingByField = request.Values
                .GroupBy(v => v.FieldId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var pair in incomingByField.Where(p => p.Value.Count > 1))
            {
                failures.Add(new ValidationFailure(nameof(request.Values), $"Field '{pair.Key}' is duplicated in request payload."));
            }

            foreach (var inputFieldId in incomingByField.Keys)
            {
                if (!activeFields.ContainsKey(inputFieldId))
                {
                    failures.Add(new ValidationFailure(nameof(request.Values), $"Field '{inputFieldId}' does not exist in the active template."));
                }
            }

            var normalizedValues = new List<CaseDynamicFieldValueDto>();
            foreach (var field in activeFields.Values)
            {
                incomingByField.TryGetValue(field.Id, out var incomingList);
                var value = incomingList?.FirstOrDefault();

                if (value == null || IsEmpty(value, field.FieldType))
                {
                    if (field.IsRequired)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' is required."));
                    }

                    continue;
                }

                var normalized = ValidateAndNormalizeFieldValue(field, value, failures);
                if (normalized != null)
                {
                    normalizedValues.Add(normalized);
                }
            }

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }

            await _caseDynamicFieldValueRepository.ReplaceCaseValuesAsync(request.CaseId, normalizedValues, cancellationToken);

            return await _mediator.Send(new GetCaseDynamicFieldsQuery(request.CaseId), cancellationToken);
        }

        private static bool IsEmpty(CaseDynamicFieldValueDto value, DynamicFieldType fieldType)
        {
            return fieldType switch
            {
                DynamicFieldType.String or DynamicFieldType.MultiLineText => string.IsNullOrWhiteSpace(value.StringValue),
                DynamicFieldType.Number or DynamicFieldType.Decimal => !value.NumberValue.HasValue,
                DynamicFieldType.Date => !value.DateValue.HasValue,
                DynamicFieldType.Bool or DynamicFieldType.CheckBox => !value.BoolValue.HasValue,
                DynamicFieldType.Enum or DynamicFieldType.MultiSelect => value.OptionIds.Count == 0,
                _ => true
            };
        }

        private static CaseDynamicFieldValueDto? ValidateAndNormalizeFieldValue(
            DynamicFieldDefinitionDto field,
            CaseDynamicFieldValueDto input,
            ICollection<ValidationFailure> failures)
        {
            switch (field.FieldType)
            {
                case DynamicFieldType.String:
                case DynamicFieldType.MultiLineText:
                {
                    if (string.IsNullOrWhiteSpace(input.StringValue))
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' must contain a string value."));
                        return null;
                    }

                    var value = input.StringValue.Trim();

                    if (field.MinLength.HasValue && value.Length < field.MinLength.Value)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' length must be at least {field.MinLength.Value}."));
                    }

                    if (field.MaxLength.HasValue && value.Length > field.MaxLength.Value)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' length must be at most {field.MaxLength.Value}."));
                    }

                    if (!string.IsNullOrWhiteSpace(field.Regex) && !Regex.IsMatch(value, field.Regex))
                    {
                        failures.Add(new ValidationFailure(
                            field.Key,
                            field.ErrorMessage ?? $"Field '{field.Label}' does not match required format."));
                    }

                    if (input.NumberValue.HasValue || input.DateValue.HasValue || input.BoolValue.HasValue || input.OptionIds.Count > 0)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' received incompatible value types."));
                    }

                    return new CaseDynamicFieldValueDto
                    {
                        FieldId = field.Id,
                        StringValue = value
                    };
                }
                case DynamicFieldType.Number:
                case DynamicFieldType.Decimal:
                {
                    if (!input.NumberValue.HasValue)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' must contain a numeric value."));
                        return null;
                    }

                    var value = input.NumberValue.Value;
                    if (field.MinNumber.HasValue && value < field.MinNumber.Value)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' must be greater than or equal to {field.MinNumber.Value}."));
                    }

                    if (field.MaxNumber.HasValue && value > field.MaxNumber.Value)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' must be less than or equal to {field.MaxNumber.Value}."));
                    }

                    if (!string.IsNullOrWhiteSpace(input.StringValue) || input.DateValue.HasValue || input.BoolValue.HasValue || input.OptionIds.Count > 0)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' received incompatible value types."));
                    }

                    return new CaseDynamicFieldValueDto
                    {
                        FieldId = field.Id,
                        NumberValue = value
                    };
                }
                case DynamicFieldType.Date:
                {
                    if (!input.DateValue.HasValue)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' must contain a date value."));
                        return null;
                    }

                    if (!string.IsNullOrWhiteSpace(input.StringValue) || input.NumberValue.HasValue || input.BoolValue.HasValue || input.OptionIds.Count > 0)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' received incompatible value types."));
                    }

                    return new CaseDynamicFieldValueDto
                    {
                        FieldId = field.Id,
                        DateValue = input.DateValue.Value
                    };
                }
                case DynamicFieldType.Bool:
                case DynamicFieldType.CheckBox:
                {
                    if (!input.BoolValue.HasValue)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' must contain a boolean value."));
                        return null;
                    }

                    if (!string.IsNullOrWhiteSpace(input.StringValue) || input.NumberValue.HasValue || input.DateValue.HasValue || input.OptionIds.Count > 0)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' received incompatible value types."));
                    }

                    return new CaseDynamicFieldValueDto
                    {
                        FieldId = field.Id,
                        BoolValue = input.BoolValue.Value
                    };
                }
                case DynamicFieldType.Enum:
                case DynamicFieldType.MultiSelect:
                {
                    var optionIds = input.OptionIds.Distinct().ToList();
                    if (optionIds.Count == 0)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' must contain one or more options."));
                        return null;
                    }

                    if (!field.IsMultiSelect && optionIds.Count > 1)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' does not support multi-select values."));
                    }

                    var validOptionIds = field.Options
                        .Where(o => o.IsActive)
                        .Select(o => o.Id)
                        .ToHashSet();

                    foreach (var optionId in optionIds)
                    {
                        if (!validOptionIds.Contains(optionId))
                        {
                            failures.Add(new ValidationFailure(field.Key, $"Option '{optionId}' is not valid for field '{field.Label}'."));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(input.StringValue) || input.NumberValue.HasValue || input.DateValue.HasValue || input.BoolValue.HasValue)
                    {
                        failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' received incompatible value types."));
                    }

                    return new CaseDynamicFieldValueDto
                    {
                        FieldId = field.Id,
                        OptionIds = optionIds
                    };
                }
                default:
                    failures.Add(new ValidationFailure(field.Key, $"Field '{field.Label}' has unsupported field type."));
                    return null;
            }
        }
    }
}
