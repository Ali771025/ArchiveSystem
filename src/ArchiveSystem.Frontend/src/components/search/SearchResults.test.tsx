import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import { SearchResults } from '@/components/search/SearchResults';
import { useAdvancedSearchStore } from '@/stores/useAdvancedSearchStore';

describe('SearchResults', () => {
  beforeEach(() => {
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
  });

  it('shows initial helper message before first search', () => {
    render(
      <MemoryRouter>
        <SearchResults />
      </MemoryRouter>,
    );
    expect(screen.getByText('برای شروع جستجو، فرم بالا را تکمیل کنید.')).toBeInTheDocument();
  });

  it('renders result items and aggregations', () => {
    useAdvancedSearchStore.setState({
      initialLoad: false,
      results: {
        items: [
          {
            id: '8e72d4b6-30e4-4dca-9ec7-a083fb31d798',
            dossierNumber: 'D-900',
            fileName: 'contract.pdf',
            highlights: ['متن <mark>نمونه</mark>'],
          },
        ],
        totalHits: 1,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 1,
        aggregations: {
          tags: { finance: 1 },
          dossiers: { 'D-900': 1 },
        },
      },
    });

    render(
      <MemoryRouter>
        <SearchResults />
      </MemoryRouter>,
    );

    expect(screen.getByText('contract.pdf')).toBeInTheDocument();
    expect(screen.getByText(/finance \(1\)/)).toBeInTheDocument();
    expect(screen.getByText(/D-900 \(1\)/)).toBeInTheDocument();
    expect(screen.getByText('مشاهده پرونده')).toBeInTheDocument();
    expect(screen.getByText('دانلود فایل')).toBeInTheDocument();
  });

  it('shows active filters and matching result metadata', () => {
    useAdvancedSearchStore.setState((state) => ({
      searchParams: {
        ...state.searchParams,
        fromDate: '2026-07-01',
        toDate: '2026-07-26',
        createdBy: 'ali',
      },
      initialLoad: false,
      results: {
        items: [
          {
            id: '12',
            dossierNumber: 'D-901',
            fileName: 'report.pdf',
            createdAt: '2026-07-20T08:30:00Z',
            createdBy: 'ali',
            highlights: [],
          },
        ],
        totalHits: 1,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 1,
        aggregations: {
          tags: {},
          dossiers: {},
        },
      },
    }));

    render(
      <MemoryRouter>
        <SearchResults />
      </MemoryRouter>,
    );

    expect(screen.getByText(/از تاریخ:/)).toBeInTheDocument();
    expect(screen.getByText(/تا تاریخ:/)).toBeInTheDocument();
    expect(screen.getAllByText(/کاربر ثبت‌کننده: ali/)).toHaveLength(2);
    expect(screen.getByText(/تاریخ و ساعت فایل:/)).toBeInTheDocument();
  });
});
