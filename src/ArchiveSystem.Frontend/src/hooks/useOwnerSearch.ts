import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { useDebounce } from '@/hooks/useDebounce';
import { ownersApi } from '@/services/ownersApi';
import type { OwnerSearchItem } from '@/types/owners';

interface UseOwnerSearchOptions {
  take?: number;
  debounceMs?: number;
  minLength?: number;
  enabled?: boolean;
}

export const ownerSearchQueryKey = (query: string, take: number) => ['owners-search', query, take] as const;

const normalizePersianSearchText = (value: string) =>
  value
    .trim()
    .replace(/[يى]/g, 'ی')
    .replace(/ك/g, 'ک')
    .replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[۠-۩]/g, (digit) => String(digit.charCodeAt(0) - 1632))
    .replace(/\u200c/g, ' ')
    .replace(/\s+/g, ' ');

export const useOwnerSearch = (searchTerm: string, options?: UseOwnerSearchOptions) => {
  const safeTake = options?.take && options.take > 0 ? Math.min(options.take, 50) : 20;
  const minLength = options?.minLength ?? 2;
  const isEnabled = options?.enabled ?? true;
  const debouncedQuery = useDebounce(normalizePersianSearchText(searchTerm), options?.debounceMs ?? 500);
  const shouldSearch = isEnabled && debouncedQuery.length >= minLength;

  const query = useQuery({
    queryKey: ownerSearchQueryKey(debouncedQuery, safeTake),
    queryFn: () => ownersApi.searchOwners(debouncedQuery, safeTake),
    enabled: shouldSearch,
    placeholderData: keepPreviousData,
  });

  const results = useMemo<OwnerSearchItem[]>(() => query.data ?? [], [query.data]);

  return {
    ...query,
    debouncedQuery,
    shouldSearch,
    results,
  };
};
