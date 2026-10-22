import { AxiosError } from 'axios';
import { create } from 'zustand';
import { executeAdvancedSearchRequest } from '@/services/useAdvancedSearchApi';
import { mapApiError } from '@/services/errorMapper';
import type {
  AdvancedSearchParams,
  AdvancedSearchSortBy,
  PagedSearchResult,
  SearchResultItem,
  SortDirection,
  TagSearchMode,
} from '@/types/advancedSearch';

const defaultSearchParams: AdvancedSearchParams = {
  generalSearchTerm: '',
  dossierNumber: '',
  postalCode: '',
  ownerFirstName: '',
  ownerLastName: '',
  fromDate: '',
  toDate: '',
  createdBy: '',
  tags: [],
  tagMode: 'Any',
  sortBy: 'Relevance',
  sortDirection: 'Desc',
  pageNumber: 1,
  pageSize: 20,
};

const toUrlSearch = (params: AdvancedSearchParams): string => {
  const query = new URLSearchParams();
  if (params.generalSearchTerm.trim()) query.set('q', params.generalSearchTerm.trim());
  if (params.dossierNumber.trim()) query.set('dossierNumber', params.dossierNumber.trim());
  if (params.postalCode.trim()) query.set('postalCode', params.postalCode.trim());
  if (params.ownerFirstName.trim()) query.set('ownerFirstName', params.ownerFirstName.trim());
  if (params.ownerLastName.trim()) query.set('ownerLastName', params.ownerLastName.trim());
  if (params.fromDate) query.set('fromDate', params.fromDate);
  if (params.toDate) query.set('toDate', params.toDate);
  if (params.createdBy.trim()) query.set('createdBy', params.createdBy.trim());
  if (params.tags.length > 0) {
    params.tags.forEach((tag) => {
      if (tag.trim()) {
        query.append('tags', tag.trim());
      }
    });
  }
  if (params.tagMode !== 'Any') query.set('tagMode', params.tagMode);
  if (params.sortBy !== 'Relevance') query.set('sortBy', params.sortBy);
  if (params.sortDirection !== 'Desc') query.set('sortDirection', params.sortDirection);
  if (params.pageNumber > 1) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize !== 20) query.set('pageSize', String(params.pageSize));
  return query.toString();
};

const fromUrlSearch = (search: string): AdvancedSearchParams => {
  const query = new URLSearchParams(search);
  const tagModeRaw = query.get('tagMode');
  const tagMode: TagSearchMode = tagModeRaw === 'All' ? 'All' : 'Any';
  const sortByRaw = query.get('sortBy');
  const sortBy: AdvancedSearchSortBy =
    sortByRaw === 'Date' || sortByRaw === 'FileName' || sortByRaw === 'Relevance'
      ? sortByRaw
      : 'Relevance';
  const sortDirectionRaw = query.get('sortDirection');
  const sortDirection: SortDirection =
    sortDirectionRaw === 'Asc' || sortDirectionRaw === 'Desc'
      ? sortDirectionRaw
      : 'Desc';
  const pageNumber = Number.parseInt(query.get('pageNumber') ?? '1', 10);
  const pageSize = Number.parseInt(query.get('pageSize') ?? '20', 10);

  return {
    generalSearchTerm: query.get('q') ?? '',
    dossierNumber: query.get('dossierNumber') ?? '',
    postalCode: query.get('postalCode') ?? '',
    ownerFirstName: query.get('ownerFirstName') ?? '',
    ownerLastName: query.get('ownerLastName') ?? '',
    fromDate: query.get('fromDate') ?? '',
    toDate: query.get('toDate') ?? '',
    createdBy: query.get('createdBy') ?? '',
    tags: query.getAll('tags'),
    tagMode,
    sortBy,
    sortDirection,
    pageNumber: Number.isFinite(pageNumber) && pageNumber > 0 ? pageNumber : 1,
    pageSize: Number.isFinite(pageSize) && pageSize > 0 ? Math.min(pageSize, 100) : 20,
  };
};

let activeController: AbortController | null = null;

interface AdvancedSearchStoreState {
  searchParams: AdvancedSearchParams;
  results: PagedSearchResult<SearchResultItem> | null;
  isLoading: boolean;
  error: string | null;
  initialLoad: boolean;
  setSearchParam: <K extends keyof AdvancedSearchParams>(key: K, value: AdvancedSearchParams[K]) => void;
  setSearchParams: (next: Partial<AdvancedSearchParams>) => void;
  executeSearch: (isNewSearch?: boolean) => Promise<void>;
  resetSearch: () => void;
  syncStateFromUrl: () => Promise<void>;
}

export const useAdvancedSearchStore = create<AdvancedSearchStoreState>((set, get) => ({
  searchParams: defaultSearchParams,
  results: null,
  isLoading: false,
  error: null,
  initialLoad: true,
  setSearchParam: (key, value) =>
    set((state) => ({
      searchParams: {
        ...state.searchParams,
        [key]: value,
      },
    })),
  setSearchParams: (next) =>
    set((state) => ({
      searchParams: {
        ...state.searchParams,
        ...next,
      },
    })),
  executeSearch: async (isNewSearch = false) => {
    const current = get().searchParams;
    const nextParams: AdvancedSearchParams = isNewSearch
      ? { ...current, pageNumber: 1 }
      : current;

    set({
      searchParams: nextParams,
      results: null,
      isLoading: true,
      error: null,
      initialLoad: false,
    });

    const qs = toUrlSearch(nextParams);
    if (typeof window !== 'undefined') {
      const nextUrl = qs ? `${window.location.pathname}?${qs}` : window.location.pathname;
      window.history.pushState({}, '', nextUrl);
    }

    activeController?.abort();
    const controller = new AbortController();
    activeController = controller;

    try {
      const response = await executeAdvancedSearchRequest(nextParams, controller.signal);
      if (activeController === controller) {
        set({ results: response, isLoading: false });
        activeController = null;
      }
    } catch (error) {
      if (error instanceof AxiosError && error.code === 'ERR_CANCELED') {
        if (activeController === controller) {
          set({ isLoading: false });
          activeController = null;
        }
        return;
      }

      if (activeController !== controller) {
        return;
      }

      const mapped = mapApiError(error);
      set({
        isLoading: false,
        error: mapped.message,
      });
      activeController = null;
    }
  },
  resetSearch: () => {
    activeController?.abort();
    activeController = null;

    set({
      searchParams: defaultSearchParams,
      results: null,
      isLoading: false,
      error: null,
      initialLoad: true,
    });

    if (typeof window !== 'undefined') {
      window.history.pushState({}, '', window.location.pathname);
    }
  },
  syncStateFromUrl: async () => {
    if (typeof window === 'undefined') {
      return;
    }

    const urlParams = fromUrlSearch(window.location.search);
    const hasSearchCriteria =
      urlParams.generalSearchTerm.trim().length > 0 ||
      urlParams.dossierNumber.trim().length > 0 ||
      urlParams.postalCode.trim().length > 0 ||
      urlParams.ownerFirstName.trim().length > 0 ||
      urlParams.ownerLastName.trim().length > 0 ||
      urlParams.fromDate.length > 0 ||
      urlParams.toDate.length > 0 ||
      urlParams.createdBy.trim().length > 0 ||
      urlParams.tags.length > 0;

    set({
      searchParams: urlParams,
      results: null,
      error: null,
      initialLoad: !hasSearchCriteria,
    });

    if (hasSearchCriteria) {
      await get().executeSearch(false);
    }
  },
}));
