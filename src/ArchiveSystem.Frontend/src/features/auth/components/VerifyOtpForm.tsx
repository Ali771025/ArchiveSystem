import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Alert, Box, Button, Stack, Typography } from '@mui/material';
import { OtpInput } from '@/features/auth/components/OtpInput';
import {
  VerifyOtpSchema,
  normalizeIranianMobile,
  verifyOtpSchema,
} from '@/features/auth/schemas/loginSchemas';
import {
  mapAuthErrorMessage,
  useRequestOtpMutation,
  useVerifyOtpAndLoginMutation,
  VerifyOtpAndLoginResponse,
} from '@/features/auth/hooks/useOtpAuthMutations';
import { useToastStore } from '@/stores/useToastStore';

interface VerifyOtpFormProps {
  mobileNumber: string;
  onBack: () => void;
  onVerified: (result: VerifyOtpAndLoginResponse) => void;
  initialExpiresAtEpochMs?: number | null;
  onResendSucceeded?: (expiresAtEpochMs: number) => void;
}

const OTP_LENGTH = 5;
const OTP_WINDOW_SECONDS = 120;

const maskMobile = (mobile: string): string => {
  const normalized = normalizeIranianMobile(mobile);
  if (normalized.length < 10) {
    return normalized;
  }

  return `${normalized.slice(0, 4)}******${normalized.slice(-2)}`;
};

const getInitialRemainingSeconds = (expiresAtEpochMs?: number | null): number => {
  if (!expiresAtEpochMs) {
    return OTP_WINDOW_SECONDS;
  }

  const diffSeconds = Math.ceil((expiresAtEpochMs - Date.now()) / 1000);
  return Math.max(0, Math.min(OTP_WINDOW_SECONDS, diffSeconds));
};

export const VerifyOtpForm = ({
  mobileNumber,
  onBack,
  onVerified,
  initialExpiresAtEpochMs,
  onResendSucceeded,
}: VerifyOtpFormProps) => {
  const pushToast = useToastStore((state) => state.push);

  const verifyMutation = useVerifyOtpAndLoginMutation();
  const resendMutation = useRequestOtpMutation();

  const [otpCode, setOtpCode] = useState('');
  const [remainingSeconds, setRemainingSeconds] = useState(() => getInitialRemainingSeconds(initialExpiresAtEpochMs));

  const {
    setValue,
    handleSubmit,
    formState: { errors },
  } = useForm<VerifyOtpSchema>({
    resolver: zodResolver(verifyOtpSchema),
    defaultValues: {
      otpCode: '',
    },
  });

  useEffect(() => {
    setValue('otpCode', otpCode, {
      shouldDirty: true,
      shouldTouch: true,
      shouldValidate: true,
    });
  }, [otpCode, setValue]);

  useEffect(() => {
    if (remainingSeconds <= 0) {
      return;
    }

    const timerId = window.setInterval(() => {
      setRemainingSeconds((previous) => {
        if (previous <= 1) {
          window.clearInterval(timerId);
          return 0;
        }

        return previous - 1;
      });
    }, 1000);

    return () => {
      window.clearInterval(timerId);
    };
  }, [remainingSeconds]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      const result = await verifyMutation.mutateAsync({
        mobileNumber,
        otpCode: values.otpCode,
      });

      if (!result.succeeded || !result.accessToken) {
        pushToast(result.message || 'کد وارد شده صحیح نیست.', 'error');
        return;
      }

      onVerified(result);
    } catch (error) {
      pushToast(mapAuthErrorMessage(error), 'error');
    }
  });

  const handleResend = async () => {
    if (remainingSeconds > 0 || resendMutation.isPending) {
      return;
    }

    try {
      const result = await resendMutation.mutateAsync({ mobileNumber });
      if (!result.succeeded) {
        throw new Error(result.message || 'ارسال مجدد کد تایید انجام نشد.');
      }

      const serverExpiresAt = result.nextAllowedAtUtc
        ? Date.parse(result.nextAllowedAtUtc)
        : Number.NaN;
      const nextExpiresAt = Number.isFinite(serverExpiresAt)
        ? serverExpiresAt
        : Date.now() + OTP_WINDOW_SECONDS * 1000;
      const nextRemainingSeconds = Math.max(
        0,
        Math.min(OTP_WINDOW_SECONDS, Math.ceil((nextExpiresAt - Date.now()) / 1000)),
      );

      setRemainingSeconds(nextRemainingSeconds);
      setOtpCode('');
      onResendSucceeded?.(nextExpiresAt);
      pushToast(result.message || 'کد تایید مجددا ارسال شد.', 'success');
    } catch (error) {
      pushToast(mapAuthErrorMessage(error), 'error');
    }
  };

  const mm = String(Math.floor(remainingSeconds / 60)).padStart(2, '0');
  const ss = String(remainingSeconds % 60).padStart(2, '0');

  const resendLabel = useMemo(() => {
    if (remainingSeconds > 0) {
      return `ارسال مجدد (${mm}:${ss})`;
    }

    if (resendMutation.isPending) {
      return 'در حال ارسال...';
    }

    return 'ارسال مجدد کد';
  }, [mm, remainingSeconds, resendMutation.isPending, ss]);

  return (
    <Stack component="form" onSubmit={onSubmit} spacing={2} noValidate dir="rtl">
      <Typography variant="body2" color="text.secondary">
        کد تایید به شماره <bdi style={{ fontWeight: 700 }}>{maskMobile(mobileNumber)}</bdi> ارسال شد.
      </Typography>

      <Stack spacing={0.75}>
        <Typography variant="subtitle2" fontWeight={700}>کد تایید ۵ رقمی</Typography>
        <OtpInput
          length={OTP_LENGTH}
          value={otpCode}
          onChange={setOtpCode}
          disabled={verifyMutation.isPending}
          autoFocus
        />
        {errors.otpCode?.message ? (
          <Typography variant="caption" color="error" role="alert">
            {errors.otpCode.message}
          </Typography>
        ) : null}
      </Stack>

      {verifyMutation.isError ? <Alert severity="error">خطا در تایید کد.</Alert> : null}

      <Button
        type="submit"
        variant="contained"
        disabled={verifyMutation.isPending || otpCode.length !== OTP_LENGTH}
        fullWidth
      >
        {verifyMutation.isPending ? 'در حال بررسی...' : 'تایید و ورود'}
      </Button>

      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1 }}>
        <Button type="button" onClick={onBack} variant="text" color="inherit">
          تغییر شماره
        </Button>

        <Button
          type="button"
          onClick={handleResend}
          disabled={remainingSeconds > 0 || resendMutation.isPending}
          variant="text"
        >
          {resendLabel}
        </Button>
      </Box>
    </Stack>
  );
};
