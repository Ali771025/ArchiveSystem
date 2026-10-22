export interface ApiResult<T> {
  success: boolean;
  errorCode?: string;
  message?: string;
  data?: T | null;
  errors?: Record<string, string[]>;
}

export const isApiResult = <T>(payload: ApiResult<T> | T): payload is ApiResult<T> => {
  return typeof payload === 'object' && payload !== null && 'success' in payload;
};

export const unwrapApiResult = <T>(payload: ApiResult<T>, fallbackMessage: string): T => {
  if (!payload.success || payload.data === undefined || payload.data === null) {
    throw new Error(payload.message?.trim() || payload.errorCode || fallbackMessage);
  }

  return payload.data;
};

export const unwrapApiResultOrRaw = <T>(payload: ApiResult<T> | T, fallbackMessage: string): T => {
  return isApiResult(payload) ? unwrapApiResult(payload, fallbackMessage) : payload;
};
