import { useCallback, useMemo, useState } from 'react';
import { archiveApi } from '@/services/archiveApi';
import type { BehsazRecord, BehsazRecordQuery } from '@/types/archive';

export interface BehsazFilterValues {
  tBasPeopleGroupId?: 1 | 2;
  instaurationArea?: string;
  instaurationZone?: string;
  instaurationBlock?: string;
  instaurationLand?: string;
  number?: string;
  manualNumber?: string;
}

const parseIntOrUndefined = (value?: string): number | undefined => {
  if (!value?.trim()) {
    return undefined;
  }

  const parsed = Number.parseInt(value, 10);
  return Number.isFinite(parsed) ? parsed : undefined;
};

export const useBehsazSearch = (isEnabled: boolean) => {
  const [rows, setRows] = useState<BehsazRecord[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [pageIndex, setPageIndex] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [filters, setFilters] = useState<BehsazFilterValues>({});

  const search = useCallback(async (nextFilters?: BehsazFilterValues) => {
    if (!isEnabled) {
      setRows([]);
      setTotalCount(0);
      return;
    }

    const activeFilters = nextFilters ?? filters;
    const request: BehsazRecordQuery = {
      tBasPeopleGroupId: activeFilters.tBasPeopleGroupId,
      instaurationArea: parseIntOrUndefined(activeFilters.instaurationArea),
      instaurationZone: parseIntOrUndefined(activeFilters.instaurationZone),
      instaurationBlock: parseIntOrUndefined(activeFilters.instaurationBlock),
      instaurationLand: parseIntOrUndefined(activeFilters.instaurationLand),
      number: activeFilters.number?.trim() || undefined,
      manualNumber: activeFilters.manualNumber?.trim() || undefined,
      pageIndex,
      pageSize,
    };

    setIsLoading(true);
    try {
      const result = await archiveApi.searchBehsazRecords(request);
      setRows(result.items);
      setTotalCount(result.totalCount);
    } finally {
      setIsLoading(false);
    }
  }, [filters, isEnabled, pageIndex, pageSize]);

  const clear = useCallback(() => {
    setFilters({});
    setPageIndex(0);
    setRows([]);
    setTotalCount(0);
  }, []);

  return useMemo(() => ({
    rows,
    totalCount,
    isLoading,
    pageIndex,
    pageSize,
    filters,
    setFilters,
    setPageIndex,
    setPageSize,
    search,
    clear,
  }), [clear, filters, isLoading, pageIndex, pageSize, rows, search, totalCount]);
};
