export type TagSearchMode = 'Any' | 'All';
export type AdvancedSearchSortBy = 'Relevance' | 'Date' | 'FileName';
export type SortDirection = 'Desc' | 'Asc';

export interface AdvancedSearchParams {
  generalSearchTerm: string;
  dossierNumber: string;
  postalCode: string;
  ownerFirstName: string;
  ownerLastName: string;
  fromDate: string;
  toDate: string;
  createdBy: string;
  tags: string[];
  tagMode: TagSearchMode;
  sortBy: AdvancedSearchSortBy;
  sortDirection: SortDirection;
  pageNumber: number;
  pageSize: number;
}

export interface SearchResultItem {
  id: string;
  dossierNumber: string;
  fileName: string;
  createdAt?: string | null;
  createdBy?: string | null;
  highlights: string[];
  caseId?: number | null;
}

export interface AdvancedSearchUserOption {
  id: number;
  username: string;
  fullName: string;
}

export interface PagedSearchResult<T> {
  items: T[];
  totalHits: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  aggregations: AdvancedSearchAggregations;
}

export interface AdvancedSearchAggregations {
  tags: Record<string, number>;
  dossiers: Record<string, number>;
}
