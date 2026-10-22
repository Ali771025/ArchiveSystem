import { zodResolver } from '@hookform/resolvers/zod';
import {
  Autocomplete,
  Button,
  Box,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { Controller, useForm } from 'react-hook-form';
import { useEffect, useMemo, useState } from 'react';
import { z } from 'zod';
import { useCaseLoanByIdQuery } from '@/features/case-loan-management/hooks/useCaseLoansQuery';
import type { CaseLoanStatus, CreateCaseLoanPayload, UpdateCaseLoanPayload } from '@/features/case-loan-management/types';
import { JalaliDateInput } from '@/components/common/JalaliDateInputs';
import { useCaseSearch } from '@/services/archiveQueries';
import { useSessionStore } from '@/stores/useSessionStore';
import { useDebouncedValue } from '@/utils/useDebouncedValue';
import type { CaseSummary } from '@/types/archive';

const statusOptions: Array<{ value: CaseLoanStatus; label: string }> = [
  { value: 'Loaned', label: 'امانت داده شده' },
  { value: 'Returned', label: 'برگشت داده شده' },
  { value: 'Overdue', label: 'دیرکرد' },
];

const caseLoanFormSchema = z
  .object({
    borrowerFirstName: z.string().trim().min(1, 'نام دریافت‌کننده الزامی است.').max(100, 'نام نباید بیش از ۱۰۰ کاراکتر باشد.'),
    borrowerLastName: z.string().trim().min(1, 'نام خانوادگی دریافت‌کننده الزامی است.').max(100, 'نام خانوادگی نباید بیش از ۱۰۰ کاراکتر باشد.'),
    caseNumber: z.string().trim().min(1, 'شماره پرونده الزامی است.').max(50, 'شماره پرونده نباید بیش از ۵۰ کاراکتر باشد.'),
    nationalId: z.string().trim().regex(/^[0-9۰-۹٠-٩]{10}$/, 'کد ملی باید دقیقاً ۱۰ رقم باشد.'),
    contactNumber: z.string().trim().min(1, 'شماره تماس الزامی است.').max(20, 'شماره تماس نباید بیش از ۲۰ کاراکتر باشد.'),
    checkoutDate: z.string().min(1, 'تاریخ خروج پرونده الزامی است.'),
    expectedReturnDate: z.string().optional(),
    actualReturnDate: z.string().optional(),
    status: z.enum(['Loaned', 'Returned', 'Overdue']),
    receiverUnit: z.string().trim().max(150, 'واحد/منطقه دریافت‌کننده نباید بیش از ۱۵۰ کاراکتر باشد.').optional(),
    registeredBy: z.string().trim().max(150, 'ثبت‌کننده امانت نباید بیش از ۱۵۰ کاراکتر باشد.').optional(),
    description: z.string().trim().max(1000, 'توضیحات نباید بیش از ۱۰۰۰ کاراکتر باشد.').optional(),
  })
  .superRefine((values, ctx) => {
    const expected = values.expectedReturnDate?.trim();
    const checkoutDate = new Date(values.checkoutDate);
    if (expected) {
      const expectedDate = new Date(expected);

      if (expectedDate < checkoutDate) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['expectedReturnDate'],
          message: 'تاریخ بازگشت مورد انتظار نباید قبل از تاریخ خروج باشد.',
        });
      }
    }

    const actual = values.actualReturnDate?.trim();
    if (values.status === 'Returned' && !actual) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ['actualReturnDate'],
        message: 'تاریخ واقعی بازگشت برای وضعیت برگشت داده شده الزامی است.',
      });
    }

    if (actual) {
      const actualDate = new Date(actual);
      if (actualDate < checkoutDate) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          path: ['actualReturnDate'],
          message: 'تاریخ واقعی بازگشت نباید قبل از تاریخ خروج باشد.',
        });
      }
    }
  });

type CaseLoanFormValues = z.infer<typeof caseLoanFormSchema>;

interface CaseLoanModalProps {
  open: boolean;
  caseLoanId: string | null;
  onClose: () => void;
  onCreate: (payload: CreateCaseLoanPayload) => Promise<void>;
  onUpdate: (payload: UpdateCaseLoanPayload) => Promise<void>;
  isSubmitting: boolean;
}

const defaultValues: CaseLoanFormValues = {
  borrowerFirstName: '',
  borrowerLastName: '',
  caseNumber: '',
  nationalId: '',
  contactNumber: '',
  checkoutDate: '',
  expectedReturnDate: '',
  actualReturnDate: '',
  status: 'Loaned',
  receiverUnit: '',
  registeredBy: '',
  description: '',
};

const formatDateInput = (value?: string | null) => {
  if (!value) {
    return '';
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }

  return date.toISOString().slice(0, 10);
};

const toCreatePayload = (values: CaseLoanFormValues): CreateCaseLoanPayload => ({
  borrowerFirstName: values.borrowerFirstName,
  borrowerLastName: values.borrowerLastName,
  caseNumber: values.caseNumber,
  nationalId: values.nationalId,
  contactNumber: values.contactNumber,
  checkoutDate: values.checkoutDate,
  expectedReturnDate: values.expectedReturnDate?.trim() || undefined,
  actualReturnDate: values.status === 'Returned' ? values.actualReturnDate?.trim() || undefined : undefined,
  status: values.status,
  receiverUnit: values.receiverUnit?.trim() || undefined,
  registeredBy: values.registeredBy?.trim() || undefined,
  description: values.description?.trim() || undefined,
  isReturned: values.status === 'Returned',
});

export const CaseLoanModal = ({
  open,
  caseLoanId,
  onClose,
  onCreate,
  onUpdate,
  isSubmitting,
}: CaseLoanModalProps) => {
  const isEditMode = Boolean(caseLoanId);
  const caseLoanQuery = useCaseLoanByIdQuery(caseLoanId, open && isEditMode);
  const currentUser = useSessionStore((state) => state.user);
  const registrarName = useMemo(() => {
    if (!currentUser) {
      return '';
    }

    return currentUser.username;
  }, [currentUser]);
  const [caseSearchText, setCaseSearchText] = useState('');
  const debouncedCaseSearchText = useDebouncedValue(caseSearchText, 350);
  const casesQuery = useCaseSearch({
    page: 1,
    pageSize: 20,
    sortBy: 'updatedAt',
    sortDirection: 'desc',
    externalCaseNumber: debouncedCaseSearchText,
  }, open);

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<CaseLoanFormValues>({
    resolver: zodResolver(caseLoanFormSchema),
    defaultValues,
  });
  const selectedStatus = watch('status');
  const caseOptions = casesQuery.data?.items ?? [];
  const isReadOnly = Boolean(
    isEditMode &&
      caseLoanQuery.data?.actualReturnDate &&
      (caseLoanQuery.data.status === 'Returned' ||
        caseLoanQuery.data.isReturned),
  );

  const getCaseOptionLabel = (option: CaseSummary) => {
    const parts = [option.caseNumber, option.externalCaseNumber, option.classificationTitle].filter(Boolean);
    return parts.join(' - ');
  };

  useEffect(() => {
    if (!open) {
      reset(defaultValues);
      setCaseSearchText('');
      return;
    }

    if (!isEditMode) {
      reset({ ...defaultValues, registeredBy: registrarName });
      setCaseSearchText('');
      return;
    }

    if (caseLoanQuery.data) {
      reset({
        borrowerFirstName: caseLoanQuery.data.borrowerFirstName,
        borrowerLastName: caseLoanQuery.data.borrowerLastName,
        caseNumber: caseLoanQuery.data.caseNumber,
        nationalId: caseLoanQuery.data.nationalId,
        contactNumber: caseLoanQuery.data.contactNumber,
        checkoutDate: formatDateInput(caseLoanQuery.data.checkoutDate),
        expectedReturnDate: formatDateInput(caseLoanQuery.data.expectedReturnDate),
        actualReturnDate: formatDateInput(caseLoanQuery.data.actualReturnDate),
        status: caseLoanQuery.data.status ?? (caseLoanQuery.data.isReturned ? 'Returned' : 'Loaned'),
        receiverUnit: caseLoanQuery.data.receiverUnit ?? '',
        registeredBy: caseLoanQuery.data.registeredBy ?? registrarName,
        description: caseLoanQuery.data.description ?? '',
      });
      setCaseSearchText(caseLoanQuery.data.caseNumber);
    }
  }, [caseLoanQuery.data, isEditMode, open, registrarName, reset]);

  const submit = handleSubmit(async (values) => {
    const payload = toCreatePayload(values);
    if (isEditMode) {
      await onUpdate({
        ...payload,
        isReturned: payload.status === 'Returned',
      });
    } else {
      await onCreate(payload);
    }

    onClose();
  });

  const isLoadingDetails = isEditMode && caseLoanQuery.isLoading;

  return (
    <Dialog
      open={open}
      onClose={(_event, reason) => {
        if (reason === 'backdropClick' || isSubmitting) {
          return;
        }

        onClose();
      }}
      disableEscapeKeyDown
      maxWidth="md"
      fullWidth
    >
      <DialogTitle>
        {isReadOnly
          ? 'مشاهده سابقه امانت پرونده فیزیکی'
          : isEditMode
            ? 'ویرایش امانت پرونده فیزیکی'
            : 'ثبت امانت جدید پرونده فیزیکی'}
      </DialogTitle>
      <DialogContent dividers>
        {isLoadingDetails ? (
          <Stack alignItems="center" justifyContent="center" sx={{ minHeight: 260 }}>
            <CircularProgress />
          </Stack>
        ) : (
          <Stack component="form" id="case-loan-form" spacing={2} onSubmit={submit} sx={{ pt: 1 }}>
            <Paper variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
              <Stack spacing={1.5}>
                <Typography variant="subtitle1" fontWeight={700}>
                  اطلاعات پرونده و دریافت‌کننده
                </Typography>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Controller
                    name="borrowerFirstName"
                    control={control}
                    render={({ field }) => (
                      <TextField {...field} disabled={isReadOnly} label="نام دریافت‌کننده" error={Boolean(errors.borrowerFirstName)} helperText={errors.borrowerFirstName?.message} fullWidth />
                    )}
                  />
                  <Controller
                    name="caseNumber"
                    control={control}
                    render={({ field }) => (
                      <Autocomplete
                        fullWidth
                        disabled={isReadOnly}
                        options={caseOptions}
                        loading={casesQuery.isFetching}
                        value={caseOptions.find((option) => option.caseNumber === field.value) ?? null}
                        inputValue={caseSearchText}
                        onInputChange={(_event, newValue, reason) => {
                          if (reason !== 'reset') {
                            setCaseSearchText(newValue);
                          }
                        }}
                        onChange={(_event, option) => {
                          field.onChange(option?.caseNumber ?? '');
                          setCaseSearchText(option?.caseNumber ?? '');
                        }}
                        getOptionLabel={getCaseOptionLabel}
                        isOptionEqualToValue={(option, value) => option.caseNumber === value.caseNumber}
                        noOptionsText={caseSearchText.trim() ? 'پرونده‌ای یافت نشد.' : 'برای جستجو شماره پرونده را وارد کنید.'}
                        renderInput={(params) => (
                          <TextField
                            {...params}
                            label="شماره پرونده"
                            error={Boolean(errors.caseNumber)}
                            helperText={errors.caseNumber?.message ?? 'از پرونده‌های ثبت‌شده در آرشیو انتخاب شود.'}
                            fullWidth
                          />
                        )}
                      />
                    )}
                  />
                </Stack>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Controller
                    name="borrowerLastName"
                    control={control}
                    render={({ field }) => (
                      <TextField {...field} disabled={isReadOnly} label="نام خانوادگی دریافت‌کننده" error={Boolean(errors.borrowerLastName)} helperText={errors.borrowerLastName?.message} fullWidth />
                    )}
                  />
                  <Controller
                    name="nationalId"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        disabled={isReadOnly}
                        label="کد ملی"
                        error={Boolean(errors.nationalId)}
                        helperText={errors.nationalId?.message}
                        inputProps={{ inputMode: 'numeric', dir: 'ltr' }}
                        fullWidth
                      />
                    )}
                  />
                </Stack>
                <Controller
                  name="contactNumber"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      disabled={isReadOnly}
                      label="شماره تماس"
                      error={Boolean(errors.contactNumber)}
                      helperText={errors.contactNumber?.message}
                      inputProps={{ dir: 'ltr' }}
                      fullWidth
                    />
                  )}
                />
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Controller
                    name="status"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        disabled={isReadOnly}
                        select
                        label="وضعیت امانت"
                        error={Boolean(errors.status)}
                        helperText={errors.status?.message}
                        fullWidth
                      >
                        {statusOptions.map((option) => (
                          <MenuItem key={option.value} value={option.value}>
                            {option.label}
                          </MenuItem>
                        ))}
                      </TextField>
                    )}
                  />
                  <Controller
                    name="receiverUnit"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        disabled={isReadOnly}
                        label="واحد/منطقه دریافت‌کننده"
                        error={Boolean(errors.receiverUnit)}
                        helperText={errors.receiverUnit?.message}
                        fullWidth
                      />
                    )}
                  />
                </Stack>
                <Controller
                  name="registeredBy"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      disabled={isReadOnly}
                      label="ثبت‌کننده امانت"
                      error={Boolean(errors.registeredBy)}
                      helperText={errors.registeredBy?.message ?? 'این مقدار از کاربر جاری سامانه خوانده می‌شود.'}
                      InputProps={{ readOnly: true }}
                      fullWidth
                    />
                  )}
                />
              </Stack>
            </Paper>

            <Paper variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
              <Stack spacing={1.5}>
                <Typography variant="subtitle1" fontWeight={700}>
                  بازه زمانی امانت
                </Typography>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Controller
                    name="checkoutDate"
                    control={control}
                    render={({ field }) => (
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography component="label" variant="body2" fontWeight={600}>
                          تاریخ خروج پرونده
                        </Typography>
                        <JalaliDateInput
                          value={field.value}
                          onChange={field.onChange}
                          disabled={isSubmitting || isReadOnly}
                        />
                        {errors.checkoutDate?.message && (
                          <Typography variant="caption" color="error" sx={{ display: 'block', mt: 0.5, mx: 1.5 }}>
                            {errors.checkoutDate.message}
                          </Typography>
                        )}
                      </Box>
                    )}
                  />
                  <Controller
                    name="expectedReturnDate"
                    control={control}
                    render={({ field }) => (
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography component="label" variant="body2" fontWeight={600}>
                          تاریخ بازگشت مورد انتظار
                        </Typography>
                        <JalaliDateInput
                          value={field.value ?? ''}
                          onChange={field.onChange}
                          disabled={isSubmitting || isReadOnly}
                        />
                        <Typography
                          variant="caption"
                          color={errors.expectedReturnDate ? 'error' : 'text.secondary'}
                          sx={{ display: 'block', mt: 0.5, mx: 1.5 }}
                        >
                          {errors.expectedReturnDate?.message ?? 'اختیاری'}
                        </Typography>
                      </Box>
                    )}
                  />
                  <Controller
                    name="actualReturnDate"
                    control={control}
                    render={({ field }) => (
                      <Box sx={{ flex: 1, minWidth: 0 }}>
                        <Typography component="label" variant="body2" fontWeight={600}>
                          تاریخ واقعی بازگشت
                          {selectedStatus === 'Returned' ? ' *' : ''}
                        </Typography>
                        <JalaliDateInput
                          value={field.value ?? ''}
                          onChange={field.onChange}
                          disabled={isSubmitting || isReadOnly || selectedStatus !== 'Returned'}
                        />
                        <Typography
                          variant="caption"
                          color={errors.actualReturnDate ? 'error' : 'text.secondary'}
                          sx={{ display: 'block', mt: 0.5, mx: 1.5 }}
                        >
                          {errors.actualReturnDate?.message ??
                            (selectedStatus === 'Returned'
                              ? 'الزامی'
                              : 'پس از انتخاب وضعیت برگشت داده‌شده فعال می‌شود.')}
                        </Typography>
                      </Box>
                    )}
                  />
                </Stack>
              </Stack>
            </Paper>

            <Paper variant="outlined" sx={{ p: 2, borderRadius: 2 }}>
              <Stack spacing={1.5}>
                <Typography variant="subtitle1" fontWeight={700}>
                  توضیحات تکمیلی
                </Typography>
                <Controller
                  name="description"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} disabled={isReadOnly} label="توضیحات" error={Boolean(errors.description)} helperText={errors.description?.message} minRows={4} multiline fullWidth />
                  )}
                />
              </Stack>
            </Paper>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={isSubmitting}>
          {isReadOnly ? 'بستن' : 'انصراف'}
        </Button>
        {!isReadOnly && (
          <Button type="submit" form="case-loan-form" variant="contained" disabled={isSubmitting || isLoadingDetails}>
            {isSubmitting ? <CircularProgress size={20} /> : isEditMode ? 'ذخیره تغییرات' : 'ثبت امانت'}
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
};
