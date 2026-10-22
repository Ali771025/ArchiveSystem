export interface ApiResult<T> {
  success: boolean;
  errorCode?: string;
  message: string;
  data?: T;
  errors?: Record<string, string[]>;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface UserDTO {
  id: number;
  username: string;
  email: string;
  phoneNumber?: string | null;
  firstName: string;
  lastName: string;
  nationalId?: string | null;
  personnelCode?: string | null;
  isActive: boolean;
  twoFactorEnabled: boolean;
  autoBackupEnabled: boolean;
  activeDuration: string;
  createdBy?: string | null;
  createdAt: string;
  lastModifiedBy?: string | null;
  lastModified?: string | null;
}

export interface UserListItemDTO {
  id: number;
  username: string;
  email: string;
  phoneNumber?: string | null;
  firstName: string;
  lastName: string;
  nationalId?: string | null;
  personnelCode?: string | null;
  isActive: boolean;
  twoFactorEnabled: boolean;
  autoBackupEnabled: boolean;
  activeDuration: string;
}

export interface UserListQueryParams {
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDescending?: boolean;
  username?: string;
  name?: string;
  firstName?: string;
  lastName?: string;
  nationalId?: string;
  columnFilterField?: string;
  columnFilterOperator?: string;
  columnFilterValue?: string;
}

export interface CreateUserPayload {
  username: string;
  email: string;
  phoneNumber?: string;
  firstName: string;
  lastName: string;
  nationalId?: string;
  personnelCode?: string;
  activeDuration: string;
  autoBackupEnabled: boolean;
  password: string;
  roles: string[];
  twoFactorEnabled: boolean;
}

export interface UpdateUserPayload {
  username: string;
  email: string;
  phoneNumber?: string;
  firstName: string;
  lastName: string;
  nationalId?: string;
  personnelCode?: string;
  activeDuration: string;
  autoBackupEnabled: boolean;
  twoFactorEnabled: boolean;
}

export interface ResetUserPasswordPayload {
  newPassword: string;
}

export interface PersonnelOption {
  id: number;
  label: string;
  firstName: string;
  lastName: string;
  nationalId?: string;
  personnelCode?: string;
}

export interface UserFilterState {
  username: string;
  firstName: string;
  lastName: string;
  nationalId: string;
}

export interface UserFormValues {
  personnelId: number | null;
  firstName: string;
  lastName: string;
  personnelCode: string;
  nationalId: string;
  email: string;
  phoneNumber: string;
  username: string;
  password: string;
  confirmPassword: string;
  activeDuration: string;
  autoBackupEnabled: boolean;
  twoFactorEnabled: boolean;
}
