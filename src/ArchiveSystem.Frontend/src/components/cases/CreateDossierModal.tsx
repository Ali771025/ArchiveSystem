import { zodResolver } from '@hookform/resolvers/zod';
import SearchIcon from '@mui/icons-material/Search';
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField } from '@mui/material';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { BehsazSearchDialog } from '@/components/integration/BehsazSearchDialog';
import type { BehsazSearchRow } from '@/types/archive';

const caseNumberPattern = /^(?:[\p{L}][\p{L}\p{N}]*(?:[-_/.][\p{L}\p{N}]+){0,2}|[\p{N}]+(?:[-_.][\p{L}\p{N}]+)?)$/u;

const createDossierSchema = z.object({
  externalCaseNumber: z
    .string()
    .trim()
    .max(100, 'حداکثر طول شماره مرجع 100 کاراکتر است.')
    .refine(
      (value) => !value || caseNumberPattern.test(value),
      'شناسه مرجع باید با قالب مجاز ثبت شود؛ نمونه معتبر: EXT-1000 یا A-1404/001.',
    ),
  address: z
    .string()
    .trim()
    .min(1, 'نشانی الزامی است.')
    .max(500, 'حداکثر طول نشانی 500 کاراکتر است.'),
  postalCode: z
    .string()
    .trim()
    .regex(/^\d{10}$/, 'کد پستی باید 10 رقم باشد.')
    .optional()
    .or(z.literal('')),
});

export type CreateDossierFormValues = z.infer<typeof createDossierSchema>;

const normalizeDigits = (value?: string) =>
  value
    ?.replace(/[۰-۹]/g, (digit) => String(digit.charCodeAt(0) - 1776))
    .replace(/[٠-٩]/g, (digit) => String(digit.charCodeAt(0) - 1584));

export const getDossierFieldsFromBehsaz = (record: BehsazSearchRow) => {
  const address = record.address?.trim() ?? '';
  const normalizedPostalCode = normalizeDigits(record.postalCode)?.trim() ?? '';
  const postalCode = /^\d{10}$/.test(normalizedPostalCode) ? normalizedPostalCode : '';

  return {
    externalCaseNumber: (record.systemNumber || record.fileNumber).trim(),
    address,
    postalCode,
  };
};

interface CreateDossierModalProps {
  open: boolean;
  isSubmitting: boolean;
  isBehsazEnabled: boolean;
  onClose: () => void;
  onSubmit: (values: { externalCaseNumber: string; address: string; postalCode?: string }) => Promise<void> | void;
}

export const CreateDossierModal = ({ open, isSubmitting, isBehsazEnabled, onClose, onSubmit }: CreateDossierModalProps) => {
  const form = useForm<CreateDossierFormValues>({
    resolver: zodResolver(createDossierSchema),
    defaultValues: {
      externalCaseNumber: '',
      address: '',
      postalCode: '',
    },
  });
  const [behsazDialogOpen, setBehsazDialogOpen] = useState(false);
  const [behsazFieldLocks, setBehsazFieldLocks] = useState({
    address: false,
    postalCode: false,
  });

  useEffect(() => {
    if (!open) {
      form.reset();
      setBehsazFieldLocks({ address: false, postalCode: false });
    }
  }, [form, open]);

  const submit = form.handleSubmit(async (values) => {
    await onSubmit({
      externalCaseNumber: values.externalCaseNumber.trim(),
      address: values.address.trim(),
      postalCode: values.postalCode?.trim() ? values.postalCode.trim() : undefined,
    });
  });

  return (
    <>
      <Dialog open={open} onClose={isSubmitting ? undefined : onClose} fullWidth maxWidth="sm">
        <DialogTitle>ایجاد پرونده</DialogTitle>
        <DialogContent>
          <Stack spacing={1.5} sx={{ mt: 0.5 }}>
            <Controller
              name="externalCaseNumber"
              control={form.control}
              render={({ field, fieldState }) => (
                <Stack spacing={1}>
                  <TextField
                    label="شناسه مرجع"
                    {...field}
                    disabled={isBehsazEnabled}
                    error={Boolean(fieldState.error)}
                    helperText={fieldState.error?.message ?? 'اختیاری'}
                  />
                  {isBehsazEnabled ? (
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<SearchIcon />}
                      onClick={() => setBehsazDialogOpen(true)}
                      disabled={isSubmitting}
                      sx={{ alignSelf: 'flex-start' }}
                    >
                      جستجو و انتخاب از بهساز
                    </Button>
                  ) : null}
                </Stack>
              )}
            />

            <Controller
              name="address"
              control={form.control}
              render={({ field, fieldState }) => (
                <TextField
                  label="نشانی"
                  multiline
                  minRows={2}
                  {...field}
                  disabled={isSubmitting || behsazFieldLocks.address}
                  error={Boolean(fieldState.error)}
                  helperText={
                    fieldState.error?.message ??
                    (behsazFieldLocks.address ? 'از بهساز دریافت شده و غیرقابل ویرایش است.' : undefined)
                  }
                />
              )}
            />

            <Controller
              name="postalCode"
              control={form.control}
              render={({ field, fieldState }) => (
                <TextField
                  label="کد پستی"
                  inputMode="numeric"
                  {...field}
                  disabled={isSubmitting || behsazFieldLocks.postalCode}
                  error={Boolean(fieldState.error)}
                  helperText={
                    fieldState.error?.message ??
                    (behsazFieldLocks.postalCode
                      ? 'از بهساز دریافت شده و غیرقابل ویرایش است.'
                      : 'اختیاری - 10 رقم')
                  }
                />
              )}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={isSubmitting}>انصراف</Button>
          <Button variant="contained" onClick={submit} disabled={isSubmitting}>
            {isSubmitting ? 'در حال ایجاد...' : 'ایجاد'}
          </Button>
        </DialogActions>
      </Dialog>

      <BehsazSearchDialog
        open={behsazDialogOpen}
        isEnabled={isBehsazEnabled}
        onClose={() => setBehsazDialogOpen(false)}
        onRecordSelect={(record: BehsazSearchRow) => {
          const dossierFields = getDossierFieldsFromBehsaz(record);
          form.setValue('externalCaseNumber', dossierFields.externalCaseNumber, { shouldValidate: true });
          form.setValue('address', dossierFields.address, {
            shouldDirty: true,
            shouldValidate: true,
          });
          form.setValue('postalCode', dossierFields.postalCode, {
            shouldDirty: true,
            shouldValidate: true,
          });
          setBehsazFieldLocks({
            address: Boolean(dossierFields.address),
            postalCode: Boolean(dossierFields.postalCode),
          });
          setBehsazDialogOpen(false);
        }}
      />
    </>
  );
};
