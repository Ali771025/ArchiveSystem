using ArchiveSystem.Application.Common.Exceptions;
using ArchiveSystem.Application.Authorization;
using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Features.Classifications.Security;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Repositories;
using ArchiveSystem.Domain.Enums;
using FluentValidation.Results;
using MediatR;

namespace ArchiveSystem.Application.Features.DynamicFields.Commands.UpsertTemplate
{
    public sealed class UpsertTemplateCommand : IRequest<DynamicTemplateDto>
    {
        public long ClassificationId { get; set; }
        public DynamicTemplateDto Template { get; set; } = new();
    }

    public sealed class UpsertTemplateCommandHandler : IRequestHandler<UpsertTemplateCommand, DynamicTemplateDto>
    {
        private readonly IClassificationRepository _classificationRepository;
        private readonly IDynamicTemplateRepository _dynamicTemplateRepository;
        private readonly IClassificationAccessAuthorizer _authorizer;

        public UpsertTemplateCommandHandler(
            IClassificationRepository classificationRepository,
            IDynamicTemplateRepository dynamicTemplateRepository,
            IClassificationAccessAuthorizer authorizer)
        {
            _classificationRepository = classificationRepository;
            _dynamicTemplateRepository = dynamicTemplateRepository;
            _authorizer = authorizer;
        }

        public async Task<DynamicTemplateDto> Handle(UpsertTemplateCommand request, CancellationToken cancellationToken)
        {
            await _authorizer.EnsureHasPermissionAsync(Permissions.DynamicFields.Manage, cancellationToken);
            await _authorizer.EnsureCanWriteAsync(request.ClassificationId, cancellationToken);

            var classification = await _classificationRepository.GetByIdAsync(request.ClassificationId, cancellationToken);
            if (classification == null)
            {
                throw new NotFoundException($"Classification '{request.ClassificationId}' was not found.");
            }

            var existingTemplate = await _dynamicTemplateRepository.GetByClassificationIdAsync(request.ClassificationId, cancellationToken);
            var preparedTemplate = PrepareTemplate(request.Template, request.ClassificationId, existingTemplate);

            ValidateTemplate(preparedTemplate);

            return await _dynamicTemplateRepository.UpsertForClassificationAsync(
                request.ClassificationId,
                preparedTemplate,
                cancellationToken);
        }

        private static DynamicTemplateDto PrepareTemplate(
            DynamicTemplateDto input,
            long classificationId,
            DynamicTemplateDto? existingTemplate)
        {
            var template = input;
            template.ClassificationId = classificationId;
            template.Id = template.Id == Guid.Empty
                ? (existingTemplate?.Id ?? Guid.NewGuid())
                : template.Id;

            var existingGroups = existingTemplate?.Groups
                .ToDictionary(g => g.Title.Trim(), g => g, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, DynamicFieldGroupDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in template.Groups)
            {
                if (group.Id == Guid.Empty)
                {
                    if (existingGroups.TryGetValue(group.Title.Trim(), out var matchedGroup))
                    {
                        group.Id = matchedGroup.Id;
                    }
                    else
                    {
                        group.Id = Guid.NewGuid();
                    }
                }

                var matchedExistingGroup = existingTemplate?.Groups.FirstOrDefault(g => g.Id == group.Id);
                var existingFields = matchedExistingGroup?.Fields
                    .ToDictionary(f => f.Key.Trim(), f => f, StringComparer.OrdinalIgnoreCase)
                    ?? new Dictionary<string, DynamicFieldDefinitionDto>(StringComparer.OrdinalIgnoreCase);

                foreach (var field in group.Fields)
                {
                    if (!Enum.IsDefined(typeof(DynamicFieldFilterType), field.FilterType)
                        || field.FilterType == 0)
                    {
                        field.FilterType = DynamicFieldFilterType.Contains;
                    }

                    if (field.Id == Guid.Empty)
                    {
                        if (existingFields.TryGetValue(field.Key.Trim(), out var matchedField))
                        {
                            field.Id = matchedField.Id;
                        }
                        else
                        {
                            field.Id = Guid.NewGuid();
                        }
                    }

                    var matchedExistingField = matchedExistingGroup?.Fields.FirstOrDefault(f => f.Id == field.Id);
                    var existingOptions = matchedExistingField?.Options
                        .ToDictionary(o => o.Value.Trim(), o => o, StringComparer.OrdinalIgnoreCase)
                        ?? new Dictionary<string, DynamicFieldOptionDto>(StringComparer.OrdinalIgnoreCase);

                    foreach (var option in field.Options)
                    {
                        if (option.Id == Guid.Empty)
                        {
                            if (existingOptions.TryGetValue(option.Value.Trim(), out var matchedOption))
                            {
                                option.Id = matchedOption.Id;
                            }
                            else
                            {
                                option.Id = Guid.NewGuid();
                            }
                        }
                    }
                }
            }

            return template;
        }

        private static void ValidateTemplate(DynamicTemplateDto template)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(template.Name))
            {
                errors["name"] = new[] { "Template name is required." };
            }

            var fieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var groupIndex = 0; groupIndex < template.Groups.Count; groupIndex++)
            {
                var group = template.Groups[groupIndex];
                if (string.IsNullOrWhiteSpace(group.Title))
                {
                    errors[$"groups[{groupIndex}].title"] = new[] { "Group title is required." };
                }

                for (var fieldIndex = 0; fieldIndex < group.Fields.Count; fieldIndex++)
                {
                    var field = group.Fields[fieldIndex];
                    if (string.IsNullOrWhiteSpace(field.Key))
                    {
                        errors[$"groups[{groupIndex}].fields[{fieldIndex}].key"] = new[] { "Field key is required." };
                    }

                    if (string.IsNullOrWhiteSpace(field.Label))
                    {
                        errors[$"groups[{groupIndex}].fields[{fieldIndex}].label"] = new[] { "Field label is required." };
                    }

                    if (!string.IsNullOrWhiteSpace(field.Key) && !fieldKeys.Add(field.Key.Trim()))
                    {
                        errors[$"groups[{groupIndex}].fields[{fieldIndex}].key"] = new[] { "Field keys must be unique within a template." };
                    }

                    var supportsOptions = field.FieldType is Domain.Enums.DynamicFieldType.Enum or Domain.Enums.DynamicFieldType.MultiSelect;
                    if (!supportsOptions && field.Options.Count > 0)
                    {
                        errors[$"groups[{groupIndex}].fields[{fieldIndex}].options"] = new[] { "Only enum or multi-select fields can have options." };
                    }

                    if (supportsOptions && field.Options.Count == 0)
                    {
                        errors[$"groups[{groupIndex}].fields[{fieldIndex}].options"] = new[] { "Enum and multi-select fields require at least one option." };
                    }

                    if (field.FilterType == DynamicFieldFilterType.Contains
                        && field.FieldType is not (Domain.Enums.DynamicFieldType.String or Domain.Enums.DynamicFieldType.MultiLineText))
                    {
                        errors[$"groups[{groupIndex}].fields[{fieldIndex}].filterType"] =
                            new[] { "Contains filter type is supported only for text fields." };
                    }

                    if (field.FilterType is DynamicFieldFilterType.LessThan
                        or DynamicFieldFilterType.GreaterThan
                        or DynamicFieldFilterType.LessThanOrEqual
                        or DynamicFieldFilterType.GreaterThanOrEqual
                        or DynamicFieldFilterType.Range)
                    {
                        var supportsRangeAndComparison = field.FieldType is Domain.Enums.DynamicFieldType.Number
                            or Domain.Enums.DynamicFieldType.Decimal
                            or Domain.Enums.DynamicFieldType.Date;

                        if (!supportsRangeAndComparison)
                        {
                            errors[$"groups[{groupIndex}].fields[{fieldIndex}].filterType"] =
                                new[] { "Selected filter type is supported only for number/date fields." };
                        }
                    }
                }
            }

            if (errors.Count > 0)
            {
                var failures = errors
                    .SelectMany(pair => pair.Value.Select(message => new ValidationFailure(pair.Key, message)))
                    .ToList();
                throw new ValidationException(failures);
            }
        }
    }
}
