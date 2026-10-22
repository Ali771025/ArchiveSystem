import { api } from '@/services/api';
import { unwrapApiResult } from '@/services/apiResult';
import type {
  ApiResult,
  CreateUserPayload,
  PagedResponse,
  PersonnelOption,
  ResetUserPasswordPayload,
  UpdateUserPayload,
  UserDTO,
  UserListItemDTO,
  UserListQueryParams,
} from '@/features/user-management/types';

const usersBasePath = '/v1/users';

interface PersonnelSearchApiItem {
  id: number;
  label: string;
  nameOrTitle?: string;
  nationalIdentifier?: string;
}

const normalizeDigits = (value?: string) =>
  value
    ?.replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1632));

const normalizeQuery = (query: UserListQueryParams) => {
  const fullName = query.name ? query.name.trim() : '';
  const nationalId = normalizeDigits(query.nationalId)?.trim();

  return {
    pageNumber: query.pageNumber,
    pageSize: query.pageSize,
    sortBy: query.sortBy ?? 'CreatedAt',
    sortDescending: query.sortDescending ?? true,
    username: query.username?.trim() || undefined,
    name: fullName || undefined,
    firstName: query.firstName?.trim() || undefined,
    lastName: query.lastName?.trim() || undefined,
    nationalId: nationalId || undefined,
    columnFilterField: query.columnFilterField?.trim() || undefined,
    columnFilterOperator: query.columnFilterOperator?.trim() || undefined,
    columnFilterValue: query.columnFilterValue?.trim() || undefined,
  };
};

export const usersApi = {
  async getUsers(query: UserListQueryParams): Promise<PagedResponse<UserListItemDTO>> {
    const response = await api.get<ApiResult<PagedResponse<UserListItemDTO>>>(usersBasePath, {
      params: normalizeQuery(query),
    });
    return unwrapApiResult(response.data, 'دریافت فهرست کاربران با خطا مواجه شد.');
  },

  async getUserById(userId: number): Promise<UserDTO> {
    const response = await api.get<ApiResult<UserDTO>>(`${usersBasePath}/${userId}`);
    return unwrapApiResult(response.data, 'اطلاعات کاربر دریافت نشد.');
  },

  async createUser(payload: CreateUserPayload): Promise<UserDTO> {
    const response = await api.post<ApiResult<UserDTO>>(usersBasePath, payload);
    return unwrapApiResult(response.data, 'ایجاد کاربر انجام نشد.');
  },

  async updateUser(userId: number, payload: UpdateUserPayload): Promise<UserDTO> {
    const response = await api.put<ApiResult<UserDTO>>(`${usersBasePath}/${userId}`, payload);
    return unwrapApiResult(response.data, 'ویرایش کاربر انجام نشد.');
  },

  async deleteUser(userId: number): Promise<void> {
    await api.delete(`${usersBasePath}/${userId}`);
  },

  async setUserStatus(userId: number, isActive: boolean): Promise<void> {
    await api.put(`${usersBasePath}/${userId}/status`, { isActive });
  },

  async resetPassword(userId: number, payload: ResetUserPasswordPayload): Promise<void> {
    await api.put(`${usersBasePath}/${userId}/password`, payload);
  },

  async assignRoles(userId: number, roles: string[]): Promise<void> {
    await api.put(`${usersBasePath}/${userId}/roles`, { roles });
  },

  async searchPersonnel(query: string): Promise<PersonnelOption[]> {
    if (!query.trim()) {
      return [];
    }

    const response = await api.get<PersonnelSearchApiItem[]>('/owners/search', {
      params: { query: query.trim(), take: 20 },
    });
    return response.data.map((item) => ({
      id: item.id,
      label: item.label,
      firstName: (item.nameOrTitle ?? item.label).split(' ')[0] || '',
      lastName: (item.nameOrTitle ?? item.label).split(' ').slice(1).join(' ') || '',
      nationalId: item.nationalIdentifier || '',
      personnelCode: '',
    }));
  },
};
