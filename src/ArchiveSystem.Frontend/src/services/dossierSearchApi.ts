import { api } from '@/services/api';
import type {
  DossierAdvancedSearchFieldCatalog,
  DossierAdvancedSearchRequest,
  DossierAdvancedSearchResult,
} from '@/types/dossierSearch';
import { normalizeOwnerNames } from '@/utils/ownerDisplay';

export const dossierSearchApi = {
  async getAdvancedSearchFields(classificationId?: number): Promise<DossierAdvancedSearchFieldCatalog> {
    const response = await api.get<DossierAdvancedSearchFieldCatalog>('/dossiers/advanced-search/fields', {
      params: classificationId ? { classificationId } : undefined,
    });

    return response.data;
  },

  async search(request: DossierAdvancedSearchRequest): Promise<DossierAdvancedSearchResult> {
    const response = await api.post<DossierAdvancedSearchResult>('/dossiers/advanced-search', request);
    return {
      ...response.data,
      items: response.data.items.map((item) => ({
        ...item,
        ownerNames: normalizeOwnerNames(item.ownerNames),
      })),
    };
  },
};
