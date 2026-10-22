import { useCallback, useEffect, useRef } from 'react';
import { AxiosError } from 'axios';
import { api } from '@/services/api';
import { mapApiError } from '@/services/errorMapper';
import type {
  AdvancedSearchParams,
  AdvancedSearchUserOption,
  PagedSearchResult,
  SearchResultItem,
} from '@/types/advancedSearch';

const buildQueryParams = (params: AdvancedSearchParams): Record<string, unknown> => {
  const query: Record<string, unknown> = {
    pageNumber: params.pageNumber,
    pageSize: params.pageSize,
    tagMode: params.tagMode,
    sortBy: params.sortBy,
    sortDirection: params.sortDirection,
  };

  if (params.generalSearchTerm.trim()) {
    query.generalSearchTerm = params.generalSearchTerm.trim();
  }
  if (params.dossierNumber.trim()) {
    query.dossierNumber = params.dossierNumber.trim();
  }
  if (params.postalCode.trim()) {
    query.postalCode = params.postalCode.trim();
  }
  if (params.ownerFirstName.trim()) {
    query.ownerFirstName = params.ownerFirstName.trim();
  }
  if (params.ownerLastName.trim()) {
    query.ownerLastName = params.ownerLastName.trim();
  }
  if (params.fromDate) {
    query.fromDate = params.fromDate;
  }
  if (params.toDate) {
    query.toDate = params.toDate;
  }
  if (params.createdBy.trim()) {
    query.createdBy = params.createdBy.trim();
  }
  if (params.tags.length > 0) {
    query.tags = params.tags.filter((tag) => tag.trim().length > 0);
  }

  return query;
};

export const executeAdvancedSearchRequest = async (
  params: AdvancedSearchParams,
  signal?: AbortSignal,
): Promise<PagedSearchResult<SearchResultItem>> => {
  const response = await api.get<PagedSearchResult<SearchResultItem>>('/v1/files/advanced-search', {
    params: buildQueryParams(params),
    signal,
  });

  return response.data;
};

export const searchAdvancedSearchUsers = async (
  search: string,
): Promise<AdvancedSearchUserOption[]> => {
  const response = await api.get<AdvancedSearchUserOption[]>(
    '/v1/files/advanced-search/users',
    {
      params: {
        search: search.trim() || undefined,
        take: 30,
      },
    },
  );

  return response.data;
};

export const useAdvancedSearchApi = () => {
  const abortRef = useRef<AbortController | null>(null);

  const cancel = useCallback(() => {
    abortRef.current?.abort();
    abortRef.current = null;
  }, []);

  const search = useCallback(async (params: AdvancedSearchParams): Promise<PagedSearchResult<SearchResultItem>> => {
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;

    try {
      return await executeAdvancedSearchRequest(params, controller.signal);
    } catch (error) {
      if (error instanceof AxiosError && error.code === 'ERR_CANCELED') {
        throw error;
      }

      throw mapApiError(error);
    }
  }, []);

  useEffect(() => cancel, [cancel]);

  return { search, cancel };
};
