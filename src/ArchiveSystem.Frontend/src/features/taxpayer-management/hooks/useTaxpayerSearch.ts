import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { useDebounce } from '@/hooks/useDebounce';
import { taxpayersApi } from '@/features/taxpayer-management/api/taxpayersApi';
import type { TaxpayerListItemDTO, TaxpayerType } from '@/features/taxpayer-management/types';
import type { OwnerType } from '@/types/owners';

interface UseTaxpayerSearchOptions {
  take?: number;
  debounceMs?: number;
  minLength?: number;
  enabled?: boolean;
}

export interface TaxpayerSearchItem {
  taxpayerId: string;
  ownerType: OwnerType;
  label: string;
  nameOrTitle: string;
  nationalIdentifier: string;
  mobile?: string | null;
}

export const taxpayerSearchQueryKey = (query: string, take: number) => ['taxpayers-search', query, take] as const;

const normalizePersianSearchText = (value: string) =>
  value
    .trim()
    .replace(/[يى]/g, 'ی')
    .replace(/ك/g, 'ک')
    .replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[۠-۩]/g, (digit) => String(digit.charCodeAt(0) - 1632))
    .replace(/\u200c/g, ' ')
    .replace(/\s+/g, ' ');

const toOwnerType = (taxpayerType: TaxpayerType): OwnerType =>
  taxpayerType === 'LegalEntity' || taxpayerType === 2 ? 2 : 1;

const mapTaxpayerToSearchItem = (taxpayer: TaxpayerListItemDTO): TaxpayerSearchItem => {
  const ownerType = toOwnerType(taxpayer.taxpayerType);
  const nameOrTitle = ownerType === 1
    ? `${taxpayer.firstName ?? ''} ${taxpayer.lastName ?? ''}`.trim()
    : (taxpayer.companyName ?? '').trim();
  const nationalIdentifier = ownerType === 1
    ? (taxpayer.nationalId ?? '').trim()
    : (taxpayer.nationalCompanyId ?? '').trim();

  return {
    taxpayerId: taxpayer.id,
    ownerType,
    label: nationalIdentifier ? `${nameOrTitle} - ${nationalIdentifier}` : nameOrTitle,
    nameOrTitle,
    nationalIdentifier,
    mobile: taxpayer.mobile ?? null,
  };
};

export const useTaxpayerSearch = (searchTerm: string, options?: UseTaxpayerSearchOptions) => {
  const safeTake = options?.take && options.take > 0 ? Math.min(options.take, 50) : 20;
  const minLength = options?.minLength ?? 2;
  const isEnabled = options?.enabled ?? true;
  const debouncedQuery = useDebounce(normalizePersianSearchText(searchTerm), options?.debounceMs ?? 500);
  const shouldSearch = isEnabled && debouncedQuery.length >= minLength;

  const query = useQuery({
    queryKey: taxpayerSearchQueryKey(debouncedQuery, safeTake),
    queryFn: () => taxpayersApi.getOwnerCandidates({
      pageNumber: 1,
      pageSize: safeTake,
      searchTerm: debouncedQuery,
    }),
    enabled: shouldSearch,
    placeholderData: keepPreviousData,
  });

  const results = useMemo<TaxpayerSearchItem[]>(
    () => query.data?.items.map(mapTaxpayerToSearchItem) ?? [],
    [query.data],
  );

  return {
    ...query,
    debouncedQuery,
    shouldSearch,
    results,
  };
};
