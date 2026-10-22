import { useMutation } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { authHttpClient } from '@/features/auth/services/authHttpClient';
import { unwrapApiResultOrRaw } from '@/services/apiResult';
import { mapApiError } from '@/services/errorMapper';
import { useToastStore } from '@/stores/useToastStore';

interface RequestOtpRequest {
  mobileNumber: string;
}

interface RequestOtpResponse {
  succeeded?: boolean;
  message?: string;
  nextAllowedAtUtc?: string;
}

interface ApiResult<T> {
  success: boolean;
  message?: string;
  data?: T;
}

const buildUserFriendlyMessage = (error: unknown): string => {
  const mapped = mapApiError(error);

  if (mapped.status === 429) {
    return 'تعداد درخواست‌ها زیاد است. لطفا چند دقیقه دیگر دوباره تلاش کنید.';
  }

  return mapped.message;
};

export const useRequestOtpMutation = () => {
  const pushToast = useToastStore((state) => state.push);

  return useMutation<RequestOtpResponse, AxiosError | Error, RequestOtpRequest>({
    mutationKey: ['auth', 'request-otp'],
    mutationFn: async (payload) => {
      const csrfResponse = await authHttpClient.get<{ token: string } | ApiResult<{ token: string }>>('/v1/auth/csrf-token');
      const csrfToken = unwrapApiResultOrRaw(csrfResponse.data, 'دریافت توکن امنیتی انجام نشد.').token;

      const response = await authHttpClient.post<RequestOtpResponse | ApiResult<RequestOtpResponse>>(
        '/v1/auth/request-otp',
        payload,
        {
          headers: {
            'X-CSRF-TOKEN': csrfToken,
          },
        },
      );

      const resolved = unwrapApiResultOrRaw(response.data, 'ارسال کد تایید انجام نشد.');
      return {
        succeeded: resolved.succeeded ?? true,
        message: resolved.message ?? 'درخواست ارسال کد ثبت شد.',
        nextAllowedAtUtc: resolved.nextAllowedAtUtc,
      };
    },
    onError: (error) => {
      pushToast(buildUserFriendlyMessage(error), 'error');
    },
  });
};
