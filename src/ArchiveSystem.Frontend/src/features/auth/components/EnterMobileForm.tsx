import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Alert, Button, Stack, TextField, Typography } from '@mui/material';
import { EnterMobileSchema, enterMobileSchema } from '@/features/auth/schemas/loginSchemas';
import { mapAuthErrorMessage, useRequestOtpMutation } from '@/features/auth/hooks/useOtpAuthMutations';
import { useToastStore } from '@/stores/useToastStore';

interface EnterMobileFormProps {
  onSuccess: (mobileNumber: string, expiresAtEpochMs: number) => void;
}

export const EnterMobileForm = ({ onSuccess }: EnterMobileFormProps) => {
  const pushToast = useToastStore((state) => state.push);
  const requestOtpMutation = useRequestOtpMutation();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<EnterMobileSchema>({
    resolver: zodResolver(enterMobileSchema),
    defaultValues: {
      mobileNumber: '',
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      const result = await requestOtpMutation.mutateAsync({
        mobileNumber: values.mobileNumber,
      });

      if (!result.succeeded) {
        throw new Error(result.message || 'ارسال کد تایید انجام نشد.');
      }

      const serverExpiresAt = result.nextAllowedAtUtc
        ? Date.parse(result.nextAllowedAtUtc)
        : Number.NaN;
      const expiresAtEpochMs = Number.isFinite(serverExpiresAt)
        ? serverExpiresAt
        : Date.now() + 120_000;

      onSuccess(values.mobileNumber, expiresAtEpochMs);
      pushToast(result.message || 'کد تایید ارسال شد.', 'success');
    } catch (error) {
      pushToast(mapAuthErrorMessage(error), 'error');
    }
  });

  return (
    <Stack component="form" onSubmit={onSubmit} spacing={2} noValidate dir="rtl">
      <Stack spacing={0.75}>
        <Typography variant="subtitle2" fontWeight={700}>
          شماره موبایل
        </Typography>
        <TextField
          id="mobileNumber"
          dir="ltr"
          inputProps={{ inputMode: 'tel' }}
          autoComplete="tel"
          placeholder="09123456789"
          size="small"
          {...register('mobileNumber')}
          error={Boolean(errors.mobileNumber)}
          helperText={errors.mobileNumber?.message}
        />
      </Stack>

      {requestOtpMutation.isError ? (
        <Alert severity="error">{mapAuthErrorMessage(requestOtpMutation.error)}</Alert>
      ) : null}

      <Button type="submit" variant="contained" disabled={requestOtpMutation.isPending} fullWidth>
        {requestOtpMutation.isPending ? 'در حال ارسال کد...' : 'ارسال کد تایید'}
      </Button>
    </Stack>
  );
};
