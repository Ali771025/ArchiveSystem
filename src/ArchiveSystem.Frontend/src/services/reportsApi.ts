import apiClient from './apiClient';
import { reportsAuditApi } from '@/features/reports/api/reportsAuditApi';
import type { AuditActionType } from '@/features/reports/types';

export interface ActivityReportItem {
  id: number;
  timestamp: string;
  actorUserId: number;
  actorName?: string | null;
  actionType: string;
  entityType: string;
  entityId: string;
  classificationId?: number | null;
  caseId?: number | null;
  fileId?: number | null;
  fileVersionId?: number | null;
  tagId?: number | null;
  ip?: string | null;
  userAgent?: string | null;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ReportSummary {
  countsByClassification: Array<{ classificationId: number; count: number }>;
  countsByActionType: Array<{ actionType: string; count: number }>;
  totalFiles: number;
  totalStorageBytes: number;
}

export interface ExportJob {
  id: string;
  createdByUserId: number;
  type: string | number;
  status: string | number;
  progressPercent?: number | null;
  errorMessage?: string | null;
  createdAt: string;
  expiresAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
}

export interface ActivityFilter {
  fromUtc?: string;
  toUtc?: string;
  actorUserId?: number;
  actionType?: string;
  classificationId?: number;
  caseId?: number;
  fileId?: number;
  tagId?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export interface FilesZipExportRequest {
  term?: string;
  fromUtc?: string;
  toUtc?: string;
  classificationId?: number;
  caseId?: number;
  fileId?: number;
  tagId?: number;
}

const toAuditActionType = (actionType?: string): AuditActionType[] | undefined => {
  if (!actionType) {
    return undefined;
  }

  const normalized = actionType.trim().toLowerCase();
  if (normalized === 'create') {
    return ['Create'];
  }

  if (normalized === 'update') {
    return ['Update'];
  }

  if (normalized === 'delete') {
    return ['Delete'];
  }

  return [actionType.trim()];
};

export const reportsApi = {
  getActivity: async (params: ActivityFilter): Promise<PaginatedResult<ActivityReportItem>> => {
    const auditResult = await reportsAuditApi.getAuditTrail({
      startDate: params.fromUtc,
      endDate: params.toUtc,
      userId: params.actorUserId != null ? String(params.actorUserId) : undefined,
      entityTypes: undefined,
      actionTypes: toAuditActionType(params.actionType),
      pageNumber: params.page ?? 1,
      pageSize: params.pageSize ?? 50,
      sortBy: params.sortBy,
      sortDirection: params.sortDescending === false ? 'asc' : 'desc',
    });

    return {
      items: auditResult.items.map((item) => ({
        id: item.auditId,
        timestamp: item.actionDate,
        actorUserId: Number(item.userId ?? 0),
        actorName: item.userId ?? null,
        actionType: item.eventType,
        entityType: item.tableName,
        entityId: item.recordId,
      })),
      totalCount: auditResult.totalCount,
      page: auditResult.pageNumber,
      pageSize: auditResult.pageSize,
      totalPages: Math.max(1, Math.ceil(auditResult.totalCount / Math.max(1, auditResult.pageSize))),
    };
  },

  getSummary: async (params: ActivityFilter): Promise<ReportSummary> => {
    const activity = await reportsAuditApi.getAuditTrail({
      startDate: params.fromUtc,
      endDate: params.toUtc,
      userId: params.actorUserId != null ? String(params.actorUserId) : undefined,
      entityTypes: undefined,
      actionTypes: toAuditActionType(params.actionType),
      pageNumber: 1,
      pageSize: 200,
      sortBy: params.sortBy,
      sortDirection: params.sortDescending === false ? 'asc' : 'desc',
    });

    const countsByAction = new Map<string, number>();
    activity.items.forEach((item) => {
      const key = item.eventType || 'Unknown';
      countsByAction.set(key, (countsByAction.get(key) ?? 0) + 1);
    });

    return {
      countsByClassification: [],
      countsByActionType: Array.from(countsByAction.entries()).map(([actionType, count]) => ({ actionType, count })),
      totalFiles: 0,
      totalStorageBytes: 0,
    };
  },

  createActivityExcelExport: async (filter: ActivityFilter): Promise<ExportJob> => {
    const response = await apiClient.post<ExportJob>('/exports/activity/excel', {
      filter: {
        fromUtc: filter.fromUtc,
        toUtc: filter.toUtc,
        actorUserId: filter.actorUserId,
        actionType: filter.actionType,
        classificationId: filter.classificationId,
        caseId: filter.caseId,
        fileId: filter.fileId,
        tagId: filter.tagId,
        page: filter.page ?? 1,
        pageSize: filter.pageSize ?? 50,
        sortBy: filter.sortBy ?? 'timestamp',
        sortDescending: filter.sortDescending ?? true,
      },
    });
    return response.data;
  },

  createFilesZipExport: async (request: FilesZipExportRequest): Promise<ExportJob> => {
    const response = await apiClient.post<ExportJob>('/exports/files/zip', request);
    return response.data;
  },

  getJobStatus: async (jobId: string): Promise<ExportJob> => {
    const response = await apiClient.get<ExportJob>(`/exports/jobs/${jobId}`);
    return response.data;
  },

  downloadJob: async (jobId: string): Promise<{ url: string; fileName: string }> => {
    const response = await apiClient.get<Blob>(`/exports/jobs/${jobId}/download`, {
      responseType: 'blob'
    });

    let fileName = `export-${jobId}`;
    const disposition = response.headers['content-disposition'] as string | undefined;
    if (disposition) {
      const match = /filename="?([^"]+)"?/i.exec(disposition);
      if (match?.[1]) {
        fileName = match[1];
      }
    }

    return {
      url: URL.createObjectURL(response.data),
      fileName
    };
  }
};

