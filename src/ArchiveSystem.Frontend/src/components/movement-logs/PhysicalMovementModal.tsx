import { zodResolver } from '@hookform/resolvers/zod';
import CloseIcon from '@mui/icons-material/Close';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import DateObject from 'react-date-object';
import { z } from 'zod';
import { ControlledJalaliDatePicker } from '@/components/movement-logs/ControlledJalaliDatePicker';
import { movementLogsApi } from '@/services/movementLogsApi';
import { mapApiError } from '@/services/errorMapper';
import { useMovementLogStore } from '@/stores/useMovementLogStore';
import { useToastStore } from '@/stores/useToastStore';

const dateValueSchema = z.custom<DateObject | null>(
  (value) => value === null || value === undefined || value instanceof DateObject,
  { message: 'فرمت تاریخ نامعتبر است.' },
);

const movementLogSchema = z
  .object({
    dossierNumber: z.string().trim().min(1, 'شماره پرونده الزامی است.').max(50, 'حداکثر 50 کاراکتر مجاز است.'),
    receiverFirstName: z.string().trim().min(1, 'نام تحویل‌گیرنده الزامی است.').max(100, 'حداکثر 100 کاراکتر مجاز است.'),
    receiverLastName: z.string().trim().min(1, 'نام خانوادگی تحویل‌گیرنده الزامی است.').max(100, 'حداکثر 100 کاراکتر مجاز است.'),
    nationalCode: z
      .string()
      .trim()
      .regex(/^\d{10}$/, 'کد ملی باید دقیقا 10 رقم باشد.'),
    phoneNumber: z.string().trim().min(1, 'شماره تماس الزامی است.').max(15, 'حداکثر 15 کاراکتر مجاز است.'),
    checkoutDate: dateValueSchema,
    expectedReturnDate: dateValueSchema.optional(),
    description: z.string().trim().max(500, 'حداکثر 500 کاراکتر مجاز است.').optional(),
  })
  .superRefine((values, context) => {
    if (!values.checkoutDate) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['checkoutDate'],
        message: 'تاریخ خروج الزامی است.',
      });
    }

    if (values.checkoutDate && values.expectedReturnDate) {
      const checkout = values.checkoutDate.toDate().getTime();
      const expectedReturn = values.expectedReturnDate.toDate().getTime();

      if (expectedReturn < checkout) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['expectedReturnDate'],
          message: 'تاریخ بازگشت نمی‌تواند قبل از تاریخ خروج باشد.',
        });
      }
    }
  });

type MovementLogFormValues = z.infer<typeof movementLogSchema>;

const defaultValues: MovementLogFormValues = {
  dossierNumber: '',
  receiverFirstName: '',
  receiverLastName: '',
  nationalCode: '',
  phoneNumber: '',
  checkoutDate: null,
  expectedReturnDate: null,
  description: '',
};

export const PhysicalMovementModal = () => {
  const isOpen = useMovementLogStore((state) => state.isOpen);
  const isLoading = useMovementLogStore((state) => state.isLoading);
  const apiError = useMovementLogStore((state) => state.error);
  const closeModal = useMovementLogStore((state) => state.closeModal);
  const setLoading = useMovementLogStore((state) => state.setLoading);
  const setError = useMovementLogStore((state) => state.setError);
  const pushToast = useToastStore((state) => state.push);

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<MovementLogFormValues>({
    resolver: zodResolver(movementLogSchema),
    defaultValues,
    mode: 'onTouched',
  });

  useEffect(() => {
    if (!isOpen) {
      reset(defaultValues);
    }
  }, [isOpen, reset]);

  const onSubmit = handleSubmit(async (data) => {
    setLoading(true);
    setError(null);

    try {
      const payload = {
        dossierNumber: data.dossierNumber.trim(),
        receiverFirstName: data.receiverFirstName.trim(),
        receiverLastName: data.receiverLastName.trim(),
        nationalCode: data.nationalCode.trim(),
        phoneNumber: data.phoneNumber.trim(),
        checkoutDate: data.checkoutDate!.toDate().toISOString(),
        expectedReturnDate: data.expectedReturnDate ? data.expectedReturnDate.toDate().toISOString() : null,
        description: data.description?.trim() ? data.description.trim() : null,
      };

      await movementLogsApi.createMovementLog(payload);
      pushToast('ثبت گردش فیزیکی پرونده با موفقیت انجام شد.', 'success');
      closeModal();
    } catch (error) {
      const mappedError = mapApiError(error);
      setError(mappedError.message);
    } finally {
      setLoading(false);
    }
  });

  return (
    <Dialog
      open={isOpen}
      onClose={isLoading ? undefined : closeModal}
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: {
          
          borderRadius: 3,
          border: '1px solid',
          borderColor: 'rgba(15, 23, 42, 0.08)',
          boxShadow: '0 24px 70px rgba(15, 23, 42, 0.18)',
          bgcolor: 'background.paper',
        },
      }}
    >
      <DialogTitle sx={{ px: 3, pt: 2.5, pb: 1.5, position: 'relative', borderBottom: '1px solid #e5e7eb' }}>
        <IconButton onClick={closeModal} disabled={isLoading} sx={{ position: 'absolute', left: 12, top: 12 }} aria-label="بستن">
          <CloseIcon />
        </IconButton>
        <Typography variant="h6" sx={{ fontWeight: 700, color: 'text.primary' }}>
          ثبت امانت پرونده فیزیکی
        </Typography>
        <Typography variant="body2" sx={{ mt: 0.75, color: 'text.secondary' }}>
          اطلاعات دریافت‌کننده و بازه امانت را ثبت کنید.
        </Typography>
      </DialogTitle>

      <DialogContent sx={{ px: 3, py: 2.5 }}>
        {apiError ? (
          <Alert severity="error" sx={{ mb: 2 }}>
            {apiError}
          </Alert>
        ) : null}

        <Box component="form" id="physical-movement-form" onSubmit={onSubmit} sx={{ pt: 0.5 }}>
          <Stack spacing={2}>
            <Paper variant="outlined" sx={{ p: { xs: 1.25, md: 1.75 }, borderRadius: 2 }}>
              <Stack spacing={1.5}>
                <Typography variant="subtitle2" fontWeight={700} color="text.primary">
                  اطلاعات پرونده و دریافت‌کننده
                </Typography>
                <Divider />
                <Box
                  sx={{
                    display: 'grid',
                    gap: 1.5,
                    gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
                  }}
                >
                  <TextField
                    label="شماره پرونده"
                    size="small"
                    {...register('dossierNumber')}
                    error={Boolean(errors.dossierNumber)}
                    helperText={errors.dossierNumber?.message}
                  />
                  <TextField
                    label="نام دریافت‌کننده"
                    size="small"
                    {...register('receiverFirstName')}
                    error={Boolean(errors.receiverFirstName)}
                    helperText={errors.receiverFirstName?.message}
                  />
                  <TextField
                    label="نام خانوادگی دریافت‌کننده"
                    size="small"
                    {...register('receiverLastName')}
                    error={Boolean(errors.receiverLastName)}
                    helperText={errors.receiverLastName?.message}
                  />
                  <TextField
                    label="کد ملی"
                    size="small"
                    inputProps={{ inputMode: 'numeric', dir: 'ltr' }}
                    {...register('nationalCode')}
                    error={Boolean(errors.nationalCode)}
                    helperText={errors.nationalCode?.message}
                  />
                  <TextField
                    label="شماره تماس"
                    size="small"
                    inputProps={{ inputMode: 'tel', dir: 'ltr' }}
                    {...register('phoneNumber')}
                    error={Boolean(errors.phoneNumber)}
                    helperText={errors.phoneNumber?.message}
                  />
                </Box>
              </Stack>
            </Paper>

            <Paper variant="outlined" sx={{ p: { xs: 1.25, md: 1.75 }, borderRadius: 2 }}>
              <Stack spacing={1.5}>
                <Typography variant="subtitle2" fontWeight={700} color="text.primary">
                  بازه زمانی امانت
                </Typography>
                <Divider />
                <Box
                  sx={{
                    display: 'grid',
                    gap: 1.5,
                    gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
                  }}
                >
                  <ControlledJalaliDatePicker control={control} name="checkoutDate" label="تاریخ خروج پرونده" />
                  <ControlledJalaliDatePicker control={control} name="expectedReturnDate" label="تاریخ بازگشت مورد انتظار (اختیاری)" />
                </Box>
              </Stack>
            </Paper>

            <Paper variant="outlined" sx={{ p: { xs: 1.25, md: 1.75 }, borderRadius: 2 }}>
              <Stack spacing={1.25}>
                <Typography variant="subtitle2" fontWeight={700} color="text.primary">
                  توضیحات تکمیلی
                </Typography>
                <Divider />
                <TextField
                  label="توضیحات"
                  size="small"
                  multiline
                  minRows={3}
                  {...register('description')}
                  error={Boolean(errors.description)}
                  helperText={errors.description?.message}
                />
              </Stack>
            </Paper>
          </Stack>
        </Box>
      </DialogContent>

      <DialogActions sx={{ px: 3, py: 2, borderTop: '1px solid #e5e7eb' }}>
        <Button onClick={closeModal} color="inherit" disabled={isLoading}>
          انصراف
        </Button>
        <Button
          type="submit"
          form="physical-movement-form"
          variant="contained"
          disabled={isLoading}
          startIcon={isLoading ? <CircularProgress size={16} color="inherit" /> : undefined}
        >
          {isLoading ? 'در حال ثبت...' : 'ثبت اطلاعات'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
