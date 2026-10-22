import { api } from '@/services/api';
import { unwrapApiResult } from '@/services/apiResult';
import { useSessionStore } from '@/stores/useSessionStore';
import { parseDateLikeValue } from '@/utils/jalaliDate';
import type {
  ApiResult,
  AuditFilterDto,
  AuditFilterOptionsDto,
  AuditLogResponseDto,
  PagedResultDto,
  ReportUserOption,
  SecurityLogFilterDto,
  SecurityLogResponseDto,
} from '@/features/reports/types';

const isApiResultEnvelope = <T>(payload: ApiResult<T> | T): payload is ApiResult<T> =>
  typeof payload === 'object' && payload !== null && 'success' in payload;

interface UsersListResult {
  items: Array<{
    id: number;
    username: string;
    firstName: string;
    lastName: string;
  }>;
}

const appendArray = (params: URLSearchParams, key: string, values?: string[]) => {
  if (!values || values.length === 0) {
    return;
  }

  values.forEach((value) => {
    params.append(key, value);
  });
};

const appendIfExists = (params: URLSearchParams, key: string, value?: string | number) => {
  if (value === undefined || value === null || value === '') {
    return;
  }

  params.append(key, String(value));
};

const toIsoBoundary = (value: string, endOfDay: boolean) => {
  if (!value) {
    return undefined;
  }

  const trimmed = value.trim();
  const date = parseDateLikeValue(trimmed);
  if (!date || Number.isNaN(date.getTime())) {
    return undefined;
  }

  // Callers that already provide an ISO timestamp expect that exact instant to reach the API.
  if (trimmed.includes('T')) {
    return date.toISOString();
  }

  if (endOfDay) {
    date.setHours(23, 59, 59, 999);
  } else {
    date.setHours(0, 0, 0, 0);
  }

  return date.toISOString();
};

const normalizePagedResult = <T>(payload: ApiResult<PagedResultDto<T>> | PagedResultDto<T>): PagedResultDto<T> => {
  if (isApiResultEnvelope<PagedResultDto<T>>(payload)) {
    const resolved = unwrapApiResult(payload, 'دریافت اطلاعات گزارش با خطا مواجه شد.');
    return {
      items: resolved?.items ?? [],
      pageNumber: resolved?.pageNumber ?? 1,
      pageSize: resolved?.pageSize ?? 25,
      totalCount: resolved?.totalCount ?? 0,
    };
  }

  return {
    items: payload.items ?? [],
    pageNumber: payload.pageNumber ?? 1,
    pageSize: payload.pageSize ?? 25,
    totalCount: payload.totalCount ?? 0,
  };
};

export const reportsAuditApi = {
  async getAuditFilterOptions(): Promise<AuditFilterOptionsDto> {
    const response = await api.get<ApiResult<AuditFilterOptionsDto> | AuditFilterOptionsDto>(
      '/v1/reports/audit-filter-options',
    );
    return isApiResultEnvelope<AuditFilterOptionsDto>(response.data)
      ? unwrapApiResult(response.data, 'دریافت گزینه‌های فیلتر گزارش با خطا مواجه شد.')
      : response.data;
  },

  async getAuditTrail(filters: AuditFilterDto): Promise<PagedResultDto<AuditLogResponseDto>> {
    const params = new URLSearchParams();

    appendIfExists(params, 'StartDate', toIsoBoundary(filters.startDate ?? '', false));
    appendIfExists(params, 'EndDate', toIsoBoundary(filters.endDate ?? '', true));
    appendIfExists(params, 'UserId', filters.userId);
    appendArray(params, 'EntityTypes', filters.entityTypes);
    appendArray(params, 'ActionTypes', filters.actionTypes);
    appendIfExists(params, 'PageNumber', filters.pageNumber);
    appendIfExists(params, 'PageSize', filters.pageSize);
    appendIfExists(params, 'SortBy', filters.sortBy);
    appendIfExists(params, 'SortDirection', filters.sortDirection);

    const response = await api.get<ApiResult<PagedResultDto<AuditLogResponseDto>> | PagedResultDto<AuditLogResponseDto>>(
      '/v1/reports/audit-trail',
      { params },
    );

    return normalizePagedResult(response.data);
  },

  async getSecurityLogs(filters: SecurityLogFilterDto): Promise<PagedResultDto<SecurityLogResponseDto>> {
    const params = new URLSearchParams();

    appendIfExists(params, 'StartDate', toIsoBoundary(filters.startDate ?? '', false));
    appendIfExists(params, 'EndDate', toIsoBoundary(filters.endDate ?? '', true));
    appendIfExists(params, 'ActorUserId', filters.actorUserId);
    appendIfExists(params, 'TargetUserId', filters.targetUserId);
    appendArray(params, 'EventTypes', filters.eventTypes);
    appendIfExists(params, 'PageNumber', filters.pageNumber);
    appendIfExists(params, 'PageSize', filters.pageSize);
    appendIfExists(params, 'SortBy', filters.sortBy);
    appendIfExists(params, 'SortDirection', filters.sortDirection);

    const response = await api.get<ApiResult<PagedResultDto<SecurityLogResponseDto>> | PagedResultDto<SecurityLogResponseDto>>(
      '/v1/reports/auth-logs',
      { params },
    );

    return normalizePagedResult(response.data);
  },

  async searchUsers(searchText: string): Promise<ReportUserOption[]> {
    const isAdmin = useSessionStore.getState().user?.roles.some(
      (role) => role.toLowerCase() === 'admin',
    );
    if (!isAdmin) {
      return [];
    }

    const params = {
      pageNumber: 1,
      pageSize: 20,
      sortBy: 'UserName',
      sortDescending: false,
      username: searchText.trim() || undefined,
    };

    const response = await api.get<ApiResult<UsersListResult> | UsersListResult>('/v1/users', { params });
    const payload = isApiResultEnvelope<UsersListResult>(response.data)
      ? unwrapApiResult(response.data, 'دریافت فهرست کاربران گزارش با خطا مواجه شد.')
      : response.data;

    return (payload?.items ?? []).map((user: UsersListResult['items'][number]) => ({
      id: user.id,
      username: user.username,
      fullName: `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim(),
      label: `${user.username} - ${`${user.firstName ?? ''} ${user.lastName ?? ''}`.trim()}`,
    }));
  },
};
