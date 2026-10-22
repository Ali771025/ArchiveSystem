export type AuditActionType = string;

export interface AuditFilterOptionsDto {
  entityTypes: string[];
  actionTypes: string[];
}

export interface AuditFilterDto {
  startDate?: string;
  endDate?: string;
  userId?: string;
  entityTypes?: string[];
  actionTypes?: AuditActionType[];
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface SecurityLogFilterDto {
  startDate?: string;
  endDate?: string;
  actorUserId?: string;
  targetUserId?: string;
  eventTypes?: string[];
  pageNumber: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: "asc" | "desc";
}

export interface AuditLogResponseDto {
  auditId: number;
  eventType: string;
  tableName: string;
  recordId: string;
  userId?: string | null;
  userName?: string | null;
  actionDate: string;
  oldValues?: string | null;
  newValues?: string | null;
  affectedColumns?: string | null;
}

export interface SecurityLogResponseDto {
  securityLogId: number;
  eventType?: string | null;
  actorUserId?: string | null;
  actorUserName?: string | null;
  targetUserId?: string | null;
  targetUserName?: string | null;
  message: string;
  clientIpAddress?: string | null;
  level?: string | null;
  actionDate: string;
  securityMetadata?: string | null;
}

export interface PagedResultDto<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
}

export interface ApiResult<T> {
  success: boolean;
  message: string;
  data?: T;
}

export interface ReportUserOption {
  id: number;
  label: string;
  username: string;
  fullName: string;
}

export interface AuditTableFilters {
  startDate: string;
  endDate: string;
  userId: string;
  entityTypes: string[];
  actionTypes: AuditActionType[];
}

export interface SecurityTableFilters {
  startDate: string;
  endDate: string;
  actorUserId: string;
  targetUserId: string;
  eventTypes: string[];
}

export interface TablePaginationState {
  pageNumber: number;
  pageSize: number;
  sortBy: string;
  sortDirection: "asc" | "desc";
}
