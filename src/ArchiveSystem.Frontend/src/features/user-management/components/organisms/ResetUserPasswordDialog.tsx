import { zodResolver } from '@hookform/resolvers/zod';
import CloseIcon from '@mui/icons-material/Close';
import SaveIcon from '@mui/icons-material/Save';
import {
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { PasswordStrengthIndicator } from '@/features/user-management/components/atoms/PasswordStrengthIndicator';
import type { UserListItemDTO } from '@/features/user-management/types';
import { mapApiError } from '@/services/errorMapper';

const resetPasswordSchema = z
  .object({
    newPassword: z
      .string()
      .min(12, 'رمز عبور باید حداقل 12 کاراکتر باشد.')
      .regex(/[A-Z]/, 'رمز عبور باید حداقل یک حرف بزرگ داشته باشد.')
      .regex(/[a-z]/, 'رمز عبور باید حداقل یک حرف کوچک داشته باشد.')
      .regex(/[0-9]/, 'رمز عبور باید حداقل یک عدد داشته باشد.')
      .regex(/[^a-zA-Z0-9]/, 'رمز عبور باید حداقل یک کاراکتر ویژه داشته باشد.'),
    confirmPassword: z.string().min(1, 'تکرار رمز عبور الزامی است.'),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'تکرار رمز عبور با رمز عبور یکسان نیست.',
  });

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

interface ResetUserPasswordDialogProps {
  open: boolean;
  user: UserListItemDTO | null;
  isSubmitting: boolean;
  onClose: () => void;
  onSubmit: (newPassword: string) => Promise<void>;
}

const defaultValues: ResetPasswordFormValues = {
  newPassword: '',
  confirmPassword: '',
};

export const ResetUserPasswordDialog = ({
  open,
  user,
  isSubmitting,
  onClose,
  onSubmit,
}: ResetUserPasswordDialogProps) => {
  const {
    register,
    handleSubmit,
    watch,
    reset,
    setError,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    mode: 'onTouched',
    defaultValues,
  });

  const newPassword = watch('newPassword');

  useEffect(() => {
    if (!open) {
      reset(defaultValues);
    }
  }, [open, reset]);

  const submit = handleSubmit(async (values) => {
    try {
      await onSubmit(values.newPassword);
      onClose();
    } catch (error) {
      const mapped = mapApiError(error);
      if (mapped.fieldErrors) {
        Object.entries(mapped.fieldErrors).forEach(([field, messages]) => {
          const firstMessage = messages[0] ?? 'رمز عبور معتبر نیست.';
          if (field.toLowerCase().includes('password')) {
            setError('newPassword', { message: firstMessage });
          }
        });
      }
    }
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="sm"
      PaperProps={{ sx: { direction: 'rtl', borderRadius: 2 } }}
      aria-labelledby="reset-user-password-dialog-title"
    >
      <DialogTitle id="reset-user-password-dialog-title" sx={{ position: 'relative' }}>
        <IconButton onClick={onClose} aria-label="بستن فرم تغییر رمز" sx={{ position: 'absolute', left: 12, top: 12 }}>
          <CloseIcon />
        </IconButton>
        <Stack spacing={0.5}>
          <Typography variant="h6" fontWeight={700}>
            تغییر رمز عبور
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {user ? `${user.firstName} ${user.lastName} - ${user.username}` : ''}
          </Typography>
        </Stack>
      </DialogTitle>

      <DialogContent dividers>
        <Stack component="form" id="reset-user-password-form" onSubmit={submit} spacing={2}>
          <TextField
            {...register('newPassword')}
            type="password"
            label="رمز عبور جدید"
            error={Boolean(errors.newPassword)}
            helperText={errors.newPassword?.message}
            fullWidth
            inputProps={{ dir: 'ltr', 'aria-label': 'رمز عبور جدید' }}
          />
          <TextField
            {...register('confirmPassword')}
            type="password"
            label="تکرار رمز عبور جدید"
            error={Boolean(errors.confirmPassword)}
            helperText={errors.confirmPassword?.message}
            fullWidth
            inputProps={{ dir: 'ltr', 'aria-label': 'تکرار رمز عبور جدید' }}
          />
          <PasswordStrengthIndicator password={newPassword} />
        </Stack>
      </DialogContent>

      <DialogActions sx={{ px: 2.5, py: 1.5 }}>
        <Button onClick={onClose} disabled={isSubmitting}>
          انصراف
        </Button>
        <Button
          type="submit"
          form="reset-user-password-form"
          variant="contained"
          disabled={isSubmitting}
          startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : <SaveIcon />}
          aria-label="ذخیره رمز عبور جدید"
        >
          ذخیره رمز جدید
        </Button>
      </DialogActions>
    </Dialog>
  );
};
