import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { reportsAuditApi } from '@/features/reports/api/reportsAuditApi';
import type { AuditFilterDto, AuditTableFilters, SecurityLogFilterDto, TablePaginationState } from '@/features/reports/types';

const reportKeys = {
  auditLogs: (filters: AuditFilterDto) =>
    [
      'reports',
      'audit-trail',
      filters.startDate ?? '',
      filters.endDate ?? '',
      filters.userId ?? '',
      filters.entityTypes?.join('|') ?? '',
      filters.actionTypes?.join('|') ?? '',
      filters.pageNumber,
      filters.pageSize,
      filters.sortBy ?? '',
      filters.sortDirection ?? 'desc',
    ] as const,
  securityLogs: (filters: SecurityLogFilterDto) =>
    [
      'reports',
      'auth-logs',
      filters.startDate ?? '',
      filters.endDate ?? '',
      filters.actorUserId ?? '',
      filters.targetUserId ?? '',
      filters.eventTypes?.join('|') ?? '',
      filters.pageNumber,
      filters.pageSize,
      filters.sortBy ?? '',
      filters.sortDirection ?? 'desc',
    ] as const,
  users: (searchText: string) => ['reports', 'actors', searchText.trim()] as const,
  auditFilterOptions: ['reports', 'audit-filter-options'] as const,
};

export const useAuditFilterOptions = () =>
  useQuery({
    queryKey: reportKeys.auditFilterOptions,
    queryFn: reportsAuditApi.getAuditFilterOptions,
    staleTime: 300000,
  });

export const useAuditLogs = (filters: AuditTableFilters, pagination: TablePaginationState) => {
  const mergedFilters: AuditFilterDto = {
    ...filters,
    ...pagination,
  };

  return useQuery({
    queryKey: reportKeys.auditLogs(mergedFilters),
    queryFn: () => reportsAuditApi.getAuditTrail(mergedFilters),
    placeholderData: keepPreviousData,
    staleTime: 20000,
  });
};

export const useSecurityLogs = (filters: SecurityLogFilterDto) => {
  return useQuery({
    queryKey: reportKeys.securityLogs(filters),
    queryFn: () => reportsAuditApi.getSecurityLogs(filters),
    placeholderData: keepPreviousData,
    staleTime: 20000,
  });
};

export const useAuditUsers = (searchText: string) => {
  return useQuery({
    queryKey: reportKeys.users(searchText),
    queryFn: () => reportsAuditApi.searchUsers(searchText),
    staleTime: 120000,
  });
};
