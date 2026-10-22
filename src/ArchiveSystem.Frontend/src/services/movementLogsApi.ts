import { api } from '@/services/api';

export interface CreateMovementLogPayload {
  dossierNumber: string;
  receiverFirstName: string;
  receiverLastName: string;
  nationalCode: string;
  phoneNumber: string;
  checkoutDate: string;
  expectedReturnDate: string | null;
  description?: string | null;
}

export interface MovementLogResponse {
  id: string;
  dossierNumber: string;
  receiverFirstName: string;
  receiverLastName: string;
  nationalCode: string;
  phoneNumber: string;
  checkoutDate: string;
  expectedReturnDate: string | null;
  description: string | null;
  createdAtUtc: string;
}

export const movementLogsApi = {
  async createMovementLog(payload: CreateMovementLogPayload): Promise<MovementLogResponse> {
    const response = await api.post<MovementLogResponse>('/movement-logs', payload);
    return response.data;
  },
};
