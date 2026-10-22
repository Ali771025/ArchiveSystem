import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useAdvancedSearchStore } from '@/stores/useAdvancedSearchStore';

const executeAdvancedSearchRequestMock = vi.fn();

vi.mock('@/services/useAdvancedSearchApi', () => ({
  executeAdvancedSearchRequest: (...args: unknown[]) => executeAdvancedSearchRequestMock(...args),
}));

describe('useAdvancedSearchStore', () => {
  beforeEach(() => {
    executeAdvancedSearchRequestMock.mockReset();
    useAdvancedSearchStore.setState({
      searchParams: {
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
      },
      results: null,
      isLoading: false,
      error: null,
      initialLoad: true,
    });

    window.history.replaceState({}, '', '/files/advanced-search');
  });

  it('executes search and updates URL/state', async () => {
    executeAdvancedSearchRequestMock.mockResolvedValue({
      items: [],
      totalHits: 0,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 0,
      aggregations: { tags: {}, dossiers: {} },
    });

    useAdvancedSearchStore.setState((state) => ({
      searchParams: {
        ...state.searchParams,
        generalSearchTerm: 'ocr test',
        tags: ['alpha'],
        sortBy: 'Date',
        sortDirection: 'Asc',
      },
    }));

    await useAdvancedSearchStore.getState().executeSearch(true);

    const { searchParams, results, isLoading } = useAdvancedSearchStore.getState();
    expect(isLoading).toBe(false);
    expect(searchParams.pageNumber).toBe(1);
    expect(results?.totalHits).toBe(0);
    expect(window.location.search).toContain('q=ocr+test');
    expect(window.location.search).toContain('sortBy=Date');
    expect(window.location.search).toContain('sortDirection=Asc');
  });

  it('syncs state from URL and triggers initial search when criteria exists', async () => {
    executeAdvancedSearchRequestMock.mockResolvedValue({
      items: [],
      totalHits: 5,
      pageNumber: 2,
      pageSize: 10,
      totalPages: 1,
      aggregations: { tags: { x: 2 }, dossiers: { d1: 1 } },
    });

    window.history.replaceState({}, '', '/files/advanced-search?q=contract&tagMode=All&sortBy=FileName&sortDirection=Asc&pageNumber=2&pageSize=10&tags=x');

    await useAdvancedSearchStore.getState().syncStateFromUrl();

    const { searchParams, initialLoad } = useAdvancedSearchStore.getState();
    expect(initialLoad).toBe(false);
    expect(searchParams.generalSearchTerm).toBe('contract');
    expect(searchParams.tagMode).toBe('All');
    expect(searchParams.sortBy).toBe('FileName');
    expect(searchParams.sortDirection).toBe('Asc');
    expect(searchParams.pageNumber).toBe(2);
    expect(searchParams.pageSize).toBe(10);
    expect(searchParams.tags).toEqual(['x']);
    expect(executeAdvancedSearchRequestMock).toHaveBeenCalled();
  });

  it('ignores a stale response when a newer OCR search has started', async () => {
    let resolveFirst!: (value: unknown) => void;
    let resolveSecond!: (value: unknown) => void;
    executeAdvancedSearchRequestMock
      .mockImplementationOnce(() => new Promise((resolve) => { resolveFirst = resolve; }))
      .mockImplementationOnce(() => new Promise((resolve) => { resolveSecond = resolve; }));

    useAdvancedSearchStore.getState().setSearchParam('generalSearchTerm', 'عبارت قدیمی');
    const firstSearch = useAdvancedSearchStore.getState().executeSearch(true);

    useAdvancedSearchStore.getState().setSearchParam('generalSearchTerm', 'عبارت دقیق OCR');
    const secondSearch = useAdvancedSearchStore.getState().executeSearch(true);

    resolveSecond({
      items: [{ id: 'new', dossierNumber: 'D-2', fileName: 'new.pdf', highlights: ['عبارت دقیق OCR'] }],
      totalHits: 1,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 1,
      aggregations: { tags: {}, dossiers: {} },
    });
    await secondSearch;

    resolveFirst({
      items: [{ id: 'old', dossierNumber: 'D-1', fileName: 'old.pdf', highlights: ['عبارت قدیمی'] }],
      totalHits: 1,
      pageNumber: 1,
      pageSize: 20,
      totalPages: 1,
      aggregations: { tags: {}, dossiers: {} },
    });
    await firstSearch;

    expect(useAdvancedSearchStore.getState().results?.items).toHaveLength(1);
    expect(useAdvancedSearchStore.getState().results?.items[0].id).toBe('new');
  });
});
