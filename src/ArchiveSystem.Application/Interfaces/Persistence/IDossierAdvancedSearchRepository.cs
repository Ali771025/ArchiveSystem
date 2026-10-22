using ArchiveSystem.Application.Features.Dossiers.AdvancedSearch;

namespace ArchiveSystem.Application.Interfaces.Persistence
{
    public interface IDossierAdvancedSearchRepository
    {
        Task<DossierAdvancedSearchFieldCatalogDto> GetAvailableFieldsAsync(
            IReadOnlyCollection<long> allowedClassificationIds,
            long? classificationId = null,
            CancellationToken cancellationToken = default);

        Task<DossierAdvancedSearchResultDto> SearchAsync(
            DossierAdvancedSearchRequestDto request,
            IReadOnlyCollection<long> allowedClassificationIds,
            CancellationToken cancellationToken = default);
    }
}
