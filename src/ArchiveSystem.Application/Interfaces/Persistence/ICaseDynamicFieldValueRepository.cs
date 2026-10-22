using ArchiveSystem.Application.Common.Models.DynamicFields;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface ICaseDynamicFieldValueRepository
    {
        Task<IReadOnlyDictionary<Guid, CaseDynamicFieldValueDto>> GetCaseValuesByCaseIdAsync(long caseId, CancellationToken cancellationToken = default);
        Task ReplaceCaseValuesAsync(long caseId, IReadOnlyCollection<CaseDynamicFieldValueDto> values, CancellationToken cancellationToken = default);
        Task<SearchCasesResultDto> SearchCasesAsync(
            IReadOnlyCollection<long> classificationIds,
            bool bypassAuthorizationFilters,
            IReadOnlyCollection<long> allowedClassificationIds,
            long? excludedClassificationId,
            string dynamicFilterLogic,
            IReadOnlyCollection<DynamicFilterGroupDto> dynamicFilterGroups,
            IReadOnlyDictionary<Guid, DynamicFieldDefinitionDto> fieldDefinitions,
            string? ownerName,
            string? externalCaseNumber,
            string? createdBy,
            DateTime? fromDate,
            DateTime? toDate,
            IReadOnlyCollection<long> tags,
            int page,
            int pageSize,
            string? sortBy,
            bool sortDescending,
            CancellationToken cancellationToken = default);
    }
}
