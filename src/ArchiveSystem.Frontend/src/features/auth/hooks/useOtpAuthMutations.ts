import { useMutation } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { authApi } from '@/services/authApi';
import { api } from '@/services/api';
import { unwrapApiResultOrRaw } from '@/services/apiResult';
import { mapApiError } from '@/services/errorMapper';

export interface RequestOtpPayload {
  mobileNumber: string;
}

export interface RequestOtpResponse {
  succeeded: boolean;
  accepted?: boolean;
  message: string;
  retryAfterSeconds?: number;
  nextAllowedAtUtc?: string;
}

export interface VerifyOtpPayload {
  mobileNumber: string;
  otpCode: string;
}

export interface VerifyOtpAndLoginResponse {
  succeeded: boolean;
  message: string;
  accessToken?: string;
  refreshToken?: string;
  expiresAtUtc?: string;
}

export interface ResetPasswordWithOtpPayload {
  mobileNumber: string;
  otpCode: string;
  newPassword: string;
}

export interface ResetPasswordWithOtpResponse {
  succeeded: boolean;
  message: string;
}

interface ApiResult<T> {
  success: boolean;
  message: string;
  errorCode?: string;
  data?: T;
}

interface CsrfTokenResponse {
  token: string;
}

let csrfTokenCache: string | null = null;

const getCsrfToken = async (): Promise<string> => {
  if (csrfTokenCache) {
    return csrfTokenCache;
  }

  const response = await api.get<CsrfTokenResponse | ApiResult<CsrfTokenResponse>>('/v1/auth/csrf-token');
  const payload = unwrapApiResultOrRaw(response.data, 'دریافت توکن امنیتی انجام نشد.');
  const token = payload.token;

  csrfTokenCache = token;
  return token;
};

const normalizeRequestOtpResponse = (response: RequestOtpResponse): RequestOtpResponse => {
  return {
    succeeded: response.succeeded ?? response.accepted ?? true,
    message: response.message ?? 'درخواست ارسال کد ثبت شد.',
    retryAfterSeconds: response.retryAfterSeconds,
    nextAllowedAtUtc: response.nextAllowedAtUtc,
  };
};

export const usePasswordLoginMutation = () => {
  return useMutation({
    mutationKey: ['auth', 'password-login'],
    mutationFn: authApi.login,
  });
};

export const useRequestOtpMutation = () => {
  return useMutation<RequestOtpResponse, AxiosError | Error, RequestOtpPayload>({
    mutationKey: ['auth', 'request-otp'],
    mutationFn: async ({ mobileNumber }) => {
      const csrfToken = await getCsrfToken();
      const response = await api.post<ApiResult<RequestOtpResponse> | RequestOtpResponse>('/v1/auth/request-otp', {
        mobileNumber,
      }, {
        headers: {
          'X-CSRF-TOKEN': csrfToken,
        },
      });

      return normalizeRequestOtpResponse(
        unwrapApiResultOrRaw(response.data, 'ارسال کد تایید انجام نشد.'),
      );
    },
  });
};

export const useVerifyOtpAndLoginMutation = () => {
  return useMutation<VerifyOtpAndLoginResponse, AxiosError | Error, VerifyOtpPayload>({
    mutationKey: ['auth', 'verify-otp-login'],
    mutationFn: async ({ mobileNumber, otpCode }) => {
      const csrfToken = await getCsrfToken();
      const response = await api.post<ApiResult<VerifyOtpAndLoginResponse> | VerifyOtpAndLoginResponse>(
        '/v1/auth/verify-otp-and-login',
        {
          mobileNumber,
          otpCode,
        },
        {
          headers: {
            'X-CSRF-TOKEN': csrfToken,
          },
        },
      );

      return unwrapApiResultOrRaw(response.data, 'تایید کد ورود انجام نشد.');
    },
  });
};

export const useRequestPasswordResetOtpMutation = () => {
  return useMutation<RequestOtpResponse, AxiosError | Error, RequestOtpPayload>({
    mutationKey: ['auth', 'request-password-reset-otp'],
    mutationFn: async ({ mobileNumber }) => {
      const csrfToken = await getCsrfToken();
      const response = await api.post<ApiResult<RequestOtpResponse> | RequestOtpResponse>('/v1/auth/request-password-reset-otp', {
        mobileNumber,
      }, {
        headers: {
          'X-CSRF-TOKEN': csrfToken,
        },
      });

      return normalizeRequestOtpResponse(
        unwrapApiResultOrRaw(response.data, 'ارسال کد بازیابی انجام نشد.'),
      );
    },
  });
};

export const useResetPasswordWithOtpMutation = () => {
  return useMutation<ResetPasswordWithOtpResponse, AxiosError | Error, ResetPasswordWithOtpPayload>({
    mutationKey: ['auth', 'reset-password-with-otp'],
    mutationFn: async ({ mobileNumber, otpCode, newPassword }) => {
      const csrfToken = await getCsrfToken();
      const response = await api.post<ApiResult<ResetPasswordWithOtpResponse> | ResetPasswordWithOtpResponse>(
        '/v1/auth/reset-password-with-otp',
        {
          mobileNumber,
          otpCode,
          newPassword,
        },
        {
          headers: {
            'X-CSRF-TOKEN': csrfToken,
          },
        },
      );

      return unwrapApiResultOrRaw(response.data, 'بازیابی گذرواژه انجام نشد.');
    },
  });
};

export const mapAuthErrorMessage = (error: unknown): string => {
  const mapped = mapApiError(error);

  if (mapped.status === 429) {
    const axiosError = error instanceof AxiosError ? error : null;
    const payload = axiosError?.response?.data as {
      retryAfterSeconds?: number;
      data?: { retryAfterSeconds?: number };
    } | undefined;
    const headerValue = axiosError?.response?.headers?.['retry-after'];
    const retryAfterSeconds = Number(
      payload?.retryAfterSeconds ?? payload?.data?.retryAfterSeconds ?? headerValue,
    );

    if (Number.isFinite(retryAfterSeconds) && retryAfterSeconds > 0) {
      return `لطفاً ${Math.ceil(retryAfterSeconds)} ثانیه صبر کنید و سپس دوباره تلاش کنید.`;
    }

    return 'تعداد درخواست‌ها زیاد است. لطفاً کمی بعد دوباره تلاش کنید.';
  }

  return mapped.message || 'خطا در پردازش درخواست. لطفا دوباره تلاش کنید.';
};
