import { api } from '@/services/api';
import type {
  AdvancedDocumentSearchRequest,
  AdvancedDocumentSearchResponse,
} from '@/types/advancedDocumentSearch';
import { formatOwnerNames } from '@/utils/ownerDisplay';

export const advancedDocumentSearchApi = {
  async searchDocuments(request: AdvancedDocumentSearchRequest): Promise<AdvancedDocumentSearchResponse> {
    const response = await api.post<AdvancedDocumentSearchResponse>('/v1/search/documents', request);
    return {
      ...response.data,
      items: response.data.items.map((item) => ({
        ...item,
        owners: formatOwnerNames(item.owners),
      })),
    };
  },
};
