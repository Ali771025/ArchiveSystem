using ArchiveSystem.Application.Common.Models.DynamicFields;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IDynamicTemplateRepository
    {
        Task<DynamicTemplateDto?> GetByClassificationIdAsync(long classificationId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DynamicTemplateDto>> GetActiveHierarchyByClassificationIdAsync(
            long classificationId,
            CancellationToken cancellationToken = default);
        Task<DynamicTemplateDto> UpsertForClassificationAsync(long classificationId, DynamicTemplateDto template, CancellationToken cancellationToken = default);
        Task<IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto>> GetFieldDefinitionsByIdsAsync(IEnumerable<Guid> fieldIds, CancellationToken cancellationToken = default);
    }
}
