import { api } from '@/services/api';
import { unwrapApiResult } from '@/services/apiResult';

export interface BackupFileDTO {
  fileName: string;
  sizeBytes: number;
  createdAtUtc: string;
  lastModifiedAtUtc: string;
}

export interface CreateBackupPayload {
  includeDatabase: boolean;
  includeFileObjects: boolean;
}

interface ApiResult<T> {
  success: boolean;
  message: string;
  data?: T;
}

const backupsBasePath = '/v1/backups';

export const backupsApi = {
  async getBackups(): Promise<BackupFileDTO[]> {
    const response = await api.get<ApiResult<BackupFileDTO[]>>(backupsBasePath);
    return unwrapApiResult(response.data, 'دریافت فهرست پشتیبان‌ها ناموفق بود.');
  },

  async createBackup(payload: CreateBackupPayload): Promise<BackupFileDTO> {
    const response = await api.post<ApiResult<BackupFileDTO>>(backupsBasePath, payload, {
      timeout: 0,
    });
    return unwrapApiResult(response.data, 'شروع پشتیبان‌گیری ناموفق بود.');
  },

  async downloadBackup(fileName: string): Promise<Blob> {
    const response = await api.get(`${backupsBasePath}/${encodeURIComponent(fileName)}`, {
      responseType: 'blob',
      timeout: 0,
    });
    return response.data;
  },
};
