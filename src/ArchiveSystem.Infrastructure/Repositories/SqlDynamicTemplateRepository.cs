using ArchiveSystem.Application.Common.Models.DynamicFields;
using ArchiveSystem.Application.Interfaces.Persistence;
using ArchiveSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchiveSystem.Infrastructure.Repositories
{
    public class SqlDynamicTemplateRepository : IDynamicTemplateRepository
    {
        private readonly ArchiveDbContext _context;

        public SqlDynamicTemplateRepository(ArchiveDbContext context)
        {
            _context = context;
        }

        public async Task<DynamicTemplateDto?> GetByClassificationIdAsync(long classificationId, CancellationToken cancellationToken = default)
        {
            var template = await _context.DynamicTemplates
                .AsNoTracking()
                .AsSplitQuery()
                .Include(t => t.Groups.OrderBy(g => g.Order))
                    .ThenInclude(g => g.Fields.OrderBy(f => f.Order))
                        .ThenInclude(f => f.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync(t => t.ClassificationId == classificationId, cancellationToken);

            return template == null ? null : MapTemplate(template);
        }

        public async Task<IReadOnlyList<DynamicTemplateDto>> GetActiveHierarchyByClassificationIdAsync(
            long classificationId,
            CancellationToken cancellationToken = default)
        {
            var classificationPath = await _context.Classifications
                .AsNoTracking()
                .Where(classification => classification.Id == classificationId && classification.DeletedAt == null)
                .Select(classification => classification.Path)
                .FirstOrDefaultAsync(cancellationToken);

            if (classificationPath == null)
            {
                return Array.Empty<DynamicTemplateDto>();
            }

            var hierarchyIds = classificationPath
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => long.TryParse(segment, out var id) ? id : (long?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (!hierarchyIds.Contains(classificationId))
            {
                hierarchyIds.Add(classificationId);
            }

            var hierarchyOrder = hierarchyIds
                .Select((id, index) => new { id, index })
                .ToDictionary(item => item.id, item => item.index);

            var templates = await _context.DynamicTemplates
                .AsNoTracking()
                .AsSplitQuery()
                .Include(template => template.Groups.OrderBy(group => group.Order))
                    .ThenInclude(group => group.Fields.OrderBy(field => field.Order))
                        .ThenInclude(field => field.Options.OrderBy(option => option.Order))
                .Where(template =>
                    hierarchyIds.Contains(template.ClassificationId) &&
                    template.IsActive &&
                    template.Classification.IsActive &&
                    template.Classification.DeletedAt == null)
                .ToListAsync(cancellationToken);

            return templates
                .OrderBy(template => hierarchyOrder[template.ClassificationId])
                .Select(MapTemplate)
                .ToList();
        }

        public async Task<DynamicTemplateDto> UpsertForClassificationAsync(long classificationId, DynamicTemplateDto template, CancellationToken cancellationToken = default)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(
                state: template,
                operation: async (_, templateState, ct) =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync(ct);

                    try
                    {
                        var existing = await _context.DynamicTemplates
                            .AsSplitQuery()
                            .Include(t => t.Groups)
                                .ThenInclude(g => g.Fields)
                                    .ThenInclude(f => f.Options)
                            .FirstOrDefaultAsync(t => t.ClassificationId == classificationId, ct);

                        if (existing == null)
                        {
                            var entity = new DynamicTemplate(
                                templateState.Id == Guid.Empty ? Guid.NewGuid() : templateState.Id,
                                classificationId,
                                templateState.Name,
                                templateState.IsActive);

                            await _context.DynamicTemplates.AddAsync(entity, ct);
                            await _context.SaveChangesAsync(ct);

                            await InsertGroupsAsync(entity.Id, templateState.Groups, ct);
                        }
                        else
                        {
                            existing.UpdateMetadata(templateState.Name, templateState.IsActive);
                            var existingGroups = existing.Groups.ToList();
                            var existingFields = existingGroups.SelectMany(g => g.Fields).ToList();
                            var existingOptions = existingFields.SelectMany(f => f.Options).ToList();

                            await SynchronizeGroupsAsync(
                                existing.Id,
                                existingGroups,
                                existingFields,
                                existingOptions,
                                templateState.Groups,
                                ct);
                        }

                        await _context.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(ct);
                        throw;
                    }

                    return true;
                },
                verifySucceeded: null,
                cancellationToken: cancellationToken);

            var upserted = await GetByClassificationIdAsync(classificationId, cancellationToken);
            return upserted ?? throw new InvalidOperationException("Failed to load dynamic template after upsert.");
        }

        private async Task SynchronizeGroupsAsync(
            Guid templateId,
            IReadOnlyCollection<DynamicFieldGroup> existingGroups,
            IReadOnlyCollection<DynamicFieldDefinition> existingFields,
            IReadOnlyCollection<DynamicFieldOption> existingOptions,
            IReadOnlyCollection<DynamicFieldGroupDto> requestedGroups,
            CancellationToken cancellationToken)
        {
            var groupsById = existingGroups.ToDictionary(group => group.Id);
            var fieldsById = existingFields.ToDictionary(field => field.Id);
            var optionsById = existingOptions.ToDictionary(option => option.Id);

            var requestedGroupIds = requestedGroups.Select(group => group.Id).ToHashSet();
            var requestedFieldIds = requestedGroups.SelectMany(group => group.Fields).Select(field => field.Id).ToHashSet();
            var requestedOptionIds = requestedGroups
                .SelectMany(group => group.Fields)
                .SelectMany(field => field.Options)
                .Select(option => option.Id)
                .ToHashSet();

            var removedOptions = existingOptions.Where(option => !requestedOptionIds.Contains(option.Id)).ToList();
            var removedFields = existingFields.Where(field => !requestedFieldIds.Contains(field.Id)).ToList();
            var removedGroups = existingGroups.Where(group => !requestedGroupIds.Contains(group.Id)).ToList();

            if (removedOptions.Count > 0)
            {
                _context.DynamicFieldOptions.RemoveRange(removedOptions);
            }

            if (removedFields.Count > 0)
            {
                _context.DynamicFieldDefinitions.RemoveRange(removedFields);
            }

            if (removedGroups.Count > 0)
            {
                _context.DynamicFieldGroups.RemoveRange(removedGroups);
            }

            foreach (var group in requestedGroups)
            {
                if (groupsById.TryGetValue(group.Id, out var groupEntity))
                {
                    groupEntity.Update(group.Title, group.Order);
                }
                else
                {
                    groupEntity = new DynamicFieldGroup(group.Id, templateId, group.Title, group.Order);
                    await _context.DynamicFieldGroups.AddAsync(groupEntity, cancellationToken);
                }

                foreach (var field in group.Fields)
                {
                    if (fieldsById.TryGetValue(field.Id, out var fieldEntity))
                    {
                        fieldEntity.Update(
                            field.Key,
                            field.Label,
                            field.FieldType,
                            field.IsRequired,
                            field.MinNumber,
                            field.MaxNumber,
                            field.MinLength,
                            field.MaxLength,
                            field.Regex,
                            field.IsMultiSelect,
                            field.ShowAsFilter,
                            field.FilterType,
                            field.Order,
                            field.IsActive,
                            field.ErrorMessage);
                    }
                    else
                    {
                        fieldEntity = CreateFieldEntity(groupEntity.Id, field);
                        await _context.DynamicFieldDefinitions.AddAsync(fieldEntity, cancellationToken);
                    }

                    foreach (var option in field.Options)
                    {
                        if (optionsById.TryGetValue(option.Id, out var optionEntity))
                        {
                            optionEntity.Update(option.Value, option.Label, option.Order, option.IsActive);
                        }
                        else
                        {
                            optionEntity = new DynamicFieldOption(
                                option.Id == Guid.Empty ? Guid.NewGuid() : option.Id,
                                fieldEntity.Id,
                                option.Value,
                                option.Label,
                                option.Order,
                                option.IsActive);

                            await _context.DynamicFieldOptions.AddAsync(optionEntity, cancellationToken);
                        }
                    }
                }
            }
        }

        public async Task<IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto>> GetFieldDefinitionsByIdsAsync(
            IEnumerable<Guid> fieldIds,
            CancellationToken cancellationToken = default)
        {
            var idList = fieldIds.Distinct().ToList();
            if (idList.Count == 0)
            {
                return new Dictionary<Guid, DynamicFieldDefinitionDto>();
            }

            var fields = await _context.DynamicFieldDefinitions
                .AsNoTracking()
                .Include(f => f.Options)
                .Where(f => idList.Contains(f.Id) && f.IsActive)
                .ToListAsync(cancellationToken);

            return fields.ToDictionary(f => f.Id, MapField);
        }

        private async Task InsertGroupsAsync(Guid templateId, IReadOnlyCollection<DynamicFieldGroupDto> groups, CancellationToken cancellationToken)
        {
            foreach (var group in groups)
            {
                var groupEntity = new DynamicFieldGroup(
                    group.Id == Guid.Empty ? Guid.NewGuid() : group.Id,
                    templateId,
                    group.Title,
                    group.Order);

                await _context.DynamicFieldGroups.AddAsync(groupEntity, cancellationToken);

                foreach (var field in group.Fields)
                {
                    var fieldEntity = CreateFieldEntity(groupEntity.Id, field);

                    await _context.DynamicFieldDefinitions.AddAsync(fieldEntity, cancellationToken);

                    foreach (var option in field.Options)
                    {
                        var optionEntity = new DynamicFieldOption(
                            option.Id == Guid.Empty ? Guid.NewGuid() : option.Id,
                            fieldEntity.Id,
                            option.Value,
                            option.Label,
                            option.Order,
                            option.IsActive);

                        await _context.DynamicFieldOptions.AddAsync(optionEntity, cancellationToken);
                    }
                }
            }
        }

        private static DynamicFieldDefinition CreateFieldEntity(Guid groupId, DynamicFieldDefinitionDto field) =>
            new(
                field.Id == Guid.Empty ? Guid.NewGuid() : field.Id,
                groupId,
                field.Key,
                field.Label,
                field.FieldType,
                field.IsRequired,
                field.MinNumber,
                field.MaxNumber,
                field.MinLength,
                field.MaxLength,
                field.Regex,
                field.IsMultiSelect,
                field.ShowAsFilter,
                field.FilterType,
                field.Order,
                field.IsActive,
                field.ErrorMessage);

        private static DynamicTemplateDto MapTemplate(DynamicTemplate template)
        {
            return new DynamicTemplateDto
            {
                Id = template.Id,
                ClassificationId = template.ClassificationId,
                Name = template.Name,
                IsActive = template.IsActive,
                Groups = template.Groups
                    .OrderBy(g => g.Order)
                    .Select(g => new DynamicFieldGroupDto
                    {
                        Id = g.Id,
                        Title = g.Title,
                        Order = g.Order,
                        Fields = g.Fields
                            .OrderBy(f => f.Order)
                            .Select(MapField)
                            .ToList()
                    })
                    .ToList()
            };
        }

        private static DynamicFieldDefinitionDto MapField(DynamicFieldDefinition field)
        {
            return new DynamicFieldDefinitionDto
            {
                Id = field.Id,
                Key = field.Key,
                Label = field.Label,
                FieldType = field.FieldType,
                IsRequired = field.IsRequired,
                MinNumber = field.MinNumber,
                MaxNumber = field.MaxNumber,
                MinLength = field.MinLength,
                MaxLength = field.MaxLength,
                Regex = field.Regex,
                ErrorMessage = field.ErrorMessage,
                IsMultiSelect = field.IsMultiSelect,
                ShowAsFilter = field.ShowAsFilter,
                FilterType = field.FilterType,
                Order = field.Order,
                IsActive = field.IsActive,
                Options = field.Options
                    .OrderBy(o => o.Order)
                    .Select(o => new DynamicFieldOptionDto
                    {
                        Id = o.Id,
                        Value = o.Value,
                        Label = o.Label,
                        Order = o.Order,
                        IsActive = o.IsActive
                    })
                    .ToList()
            };
        }
    }
}
