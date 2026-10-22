import { zodResolver } from '@hookform/resolvers/zod';
import {
  ClipboardEvent,
  ChangeEvent,
  CSSProperties,
  KeyboardEvent,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useForm } from 'react-hook-form';
import { Alert, Box, Button, Paper, Stack, Typography } from '@mui/material';
import { useResilientTimer } from '@/features/auth/hooks/useResilientTimer';
import { verifyOtpSchema, type VerifyOtpSchema } from '@/features/auth/schemas/authSchemas';
import { useToastStore } from '@/stores/useToastStore';

interface OtpVerificationStepProps {
  phoneNumber: string;
  isPending: boolean;
  resendPending: boolean;
  storageKey?: string;
  initialDurationSeconds?: number;
  onSubmitOtp: (otp: string) => Promise<void>;
  onResend: () => Promise<void>;
}

interface OTPCredential extends Credential {
  code: string;
}

interface OTPCredentialRequestOptions extends CredentialRequestOptions {
  otp: { transport: Array<'sms'> };
  signal?: AbortSignal;
}

const inputCount = 5;
const hiddenStyle: CSSProperties = {
  position: 'absolute',
  width: 1,
  height: 1,
  margin: -1,
  padding: 0,
  border: 0,
  overflow: 'hidden',
  clip: 'rect(0, 0, 0, 0)',
};

const sanitizeOtpDigits = (value: string): string => value.replace(/\D/g, '').slice(0, inputCount);

export const OtpVerificationStep = ({
  phoneNumber,
  isPending,
  resendPending,
  storageKey = 'archive:otp-resend-timer',
  initialDurationSeconds = 120,
  onSubmitOtp,
  onResend,
}: OtpVerificationStepProps) => {
  const pushToast = useToastStore((state) => state.push);

  const [digits, setDigits] = useState<string[]>(() => Array.from({ length: inputCount }, () => ''));
  const [liveMessage, setLiveMessage] = useState('');
  const inputRefs = useRef<Array<HTMLInputElement | null>>([]);

  const {
    register,
    setValue,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<VerifyOtpSchema>({
    resolver: zodResolver(verifyOtpSchema),
    defaultValues: {
      otp: '',
    },
    mode: 'onChange',
  });

  const otpValue = useMemo(() => digits.join(''), [digits]);

  const { remainingSeconds, formatted, reset: resetTimer } = useResilientTimer({
    storageKey,
    initialDurationSeconds,
    autoStart: true,
    onExpire: () => setLiveMessage('زمان ارسال مجدد فرا رسید.'),
  });

  const syncOtpValue = useCallback(
    (value: string) => {
      setValue('otp', value, {
        shouldDirty: true,
        shouldTouch: true,
        shouldValidate: true,
      });
    },
    [setValue],
  );

  useEffect(() => {
    syncOtpValue(otpValue);
  }, [otpValue, syncOtpValue]);

  const submitOtp = useCallback(
    async (payload: VerifyOtpSchema) => {
      await onSubmitOtp(payload.otp);
    },
    [onSubmitOtp],
  );

  const tryAutoSubmit = useCallback(
    (nextDigits: string[]) => {
      if (nextDigits.every((digit) => digit !== '') && !isPending) {
        void handleSubmit(submitOtp)();
      }
    },
    [handleSubmit, isPending, submitOtp],
  );

  const updateDigit = useCallback(
    (index: number, rawValue: string) => {
      const sanitized = rawValue.replace(/\D/g, '');
      if (!sanitized) {
        setDigits((prev) => {
          const next = [...prev];
          next[index] = '';
          return next;
        });
        return;
      }

      setDigits((prev) => {
        const next = [...prev];
        let cursor = index;

        for (const char of sanitized) {
          if (cursor >= inputCount) {
            break;
          }

          next[cursor] = char;
          cursor += 1;
        }

        const focusIndex = Math.min(cursor, inputCount - 1);
        inputRefs.current[focusIndex]?.focus();
        tryAutoSubmit(next);
        return next;
      });
    },
    [tryAutoSubmit],
  );

  const onKeyDown = useCallback((index: number, event: KeyboardEvent<HTMLInputElement>) => {
    const { key } = event;

    if (key === 'ArrowLeft') {
      event.preventDefault();
      inputRefs.current[Math.max(0, index - 1)]?.focus();
      return;
    }

    if (key === 'ArrowRight') {
      event.preventDefault();
      inputRefs.current[Math.min(inputCount - 1, index + 1)]?.focus();
      return;
    }

    if (key === 'Backspace') {
      event.preventDefault();
      setDigits((prev) => {
        const next = [...prev];

        if (next[index]) {
          next[index] = '';
        } else {
          const previousIndex = Math.max(0, index - 1);
          next[previousIndex] = '';
          inputRefs.current[previousIndex]?.focus();
        }

        return next;
      });
    }
  }, []);

  const onPaste = useCallback(
    (event: ClipboardEvent<HTMLDivElement>) => {
      event.preventDefault();
      const pasted = sanitizeOtpDigits(event.clipboardData.getData('text'));
      if (!pasted) {
        return;
      }

      const nextDigits = Array.from({ length: inputCount }, (_, index) => pasted[index] ?? '');
      setDigits(nextDigits);

      const nextFocus = Math.min(pasted.length, inputCount - 1);
      inputRefs.current[nextFocus]?.focus();
      tryAutoSubmit(nextDigits);
    },
    [tryAutoSubmit],
  );

  const clearAndFocusFirst = useCallback(() => {
    setDigits(Array.from({ length: inputCount }, () => ''));
    reset({ otp: '' });
    inputRefs.current[0]?.focus();
  }, [reset]);

  const handleResend = useCallback(async () => {
    if (remainingSeconds > 0 || resendPending) {
      return;
    }

    try {
      await onResend();
      clearAndFocusFirst();
      resetTimer(initialDurationSeconds);
      setLiveMessage('کد جدید ارسال شد.');
    } catch {
      pushToast('ارسال مجدد کد ناموفق بود. لطفا مجدد تلاش کنید.', 'error');
      setLiveMessage('ارسال مجدد کد ناموفق بود.');
    }
  }, [clearAndFocusFirst, initialDurationSeconds, onResend, pushToast, remainingSeconds, resendPending, resetTimer]);

  useEffect(() => {
    const credentialsApi = navigator.credentials;
    if (!credentialsApi || typeof credentialsApi.get !== 'function') {
      return;
    }

    const supportsWebOtp = 'OTPCredential' in window;
    if (!supportsWebOtp) {
      return;
    }

    const abortController = new AbortController();

    void credentialsApi
      .get({
        otp: { transport: ['sms'] },
        signal: abortController.signal,
      } as OTPCredentialRequestOptions)
      .then((credential) => {
        const typedCredential = credential as OTPCredential | null;
        const code = sanitizeOtpDigits(typedCredential?.code ?? '');
        if (code.length !== inputCount) {
          return;
        }

        const nextDigits = code.split('');
        setDigits(nextDigits);
        setLiveMessage('کد یکبار مصرف به‌صورت خودکار تکمیل شد.');
        tryAutoSubmit(nextDigits);
      })
      .catch(() => {
        // Keep manual entry flow.
      });

    return () => {
      abortController.abort();
    };
  }, [tryAutoSubmit]);

  return (
    <Paper dir="rtl" variant="outlined" sx={{ width: '100%', maxWidth: 420, borderRadius: 2.5, p: 2.5 }}>
      <div style={hiddenStyle} role="status" aria-live="polite" aria-atomic="true">
        {liveMessage}
      </div>

      <Stack spacing={2} component="form" onSubmit={handleSubmit(submitOtp)} noValidate aria-describedby={errors.otp ? 'otp-error' : undefined}>
        <Stack spacing={0.5}>
          <Typography variant="h6" fontWeight={700}>تایید رمز یکبار مصرف</Typography>
          <Typography variant="body2" color="text.secondary">
            کد 5 رقمی ارسال‌شده به <bdi>{phoneNumber}</bdi> را وارد کنید.
          </Typography>
        </Stack>

        <input type="hidden" {...register('otp')} />

        <Box dir="ltr" sx={{ display: 'flex', justifyContent: 'space-between', gap: 1 }} onPaste={onPaste}>
          {digits.map((digit, index) => (
            <Box
              key={index}
              component="input"
              ref={(element: HTMLInputElement | null) => {
                inputRefs.current[index] = element;
              }}
              inputMode="numeric"
              pattern="[0-9]*"
              maxLength={inputCount}
              autoComplete={index === 0 ? 'one-time-code' : 'off'}
              aria-label={`رقم ${index + 1} از 5`}
              value={digit}
              onChange={(event: ChangeEvent<HTMLInputElement>) => updateDigit(index, event.target.value)}
              onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => onKeyDown(index, event)}
              disabled={isPending}
              sx={{
                height: 48,
                width: 48,
                borderRadius: 1.5,
                border: '1px solid',
                borderColor: errors.otp ? 'error.main' : 'divider',
                textAlign: 'center',
                fontSize: 22,
                fontWeight: 700,
                outline: 'none',
                transition: 'all 160ms ease',
                '&:focus': {
                  borderColor: errors.otp ? 'error.main' : 'primary.main',
                  boxShadow: errors.otp
                    ? '0 0 0 3px rgba(211, 47, 47, 0.18)'
                    : '0 0 0 3px rgba(25, 118, 210, 0.16)',
                },
                '&:disabled': {
                  bgcolor: 'action.disabledBackground',
                },
              }}
            />
          ))}
        </Box>

        {errors.otp?.message ? (
          <Typography id="otp-error" role="alert" variant="caption" color="error">
            {errors.otp.message}
          </Typography>
        ) : null}

        {isPending ? <Alert severity="info">در حال بررسی کد...</Alert> : null}

        <Button type="submit" variant="contained" disabled={isPending || otpValue.length !== inputCount} fullWidth>
          {isPending ? 'در حال بررسی...' : 'تایید کد'}
        </Button>

        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 1 }}>
          <Typography variant="body2" color="text.secondary" aria-live="polite">
            {remainingSeconds > 0 ? `ارسال مجدد تا ${formatted}` : 'می‌توانید کد را مجددا ارسال کنید.'}
          </Typography>

          <Button type="button" onClick={handleResend} disabled={remainingSeconds > 0 || resendPending} variant="text" aria-busy={resendPending}>
            {resendPending ? 'در حال ارسال...' : 'ارسال مجدد'}
          </Button>
        </Box>
      </Stack>
    </Paper>
  );
};
