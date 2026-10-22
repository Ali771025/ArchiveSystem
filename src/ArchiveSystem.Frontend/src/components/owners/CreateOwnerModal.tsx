import { zodResolver } from '@hookform/resolvers/zod';
import CloseIcon from '@mui/icons-material/Close';
import CorporateFareIcon from '@mui/icons-material/CorporateFare';
import PersonIcon from '@mui/icons-material/Person';
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import { type HTMLAttributes, useEffect } from 'react';
import { Controller, type Control, useForm } from 'react-hook-form';
import { z } from 'zod';
import { useCreateOwner, useUpdateOwner } from '@/hooks/useDossierOwners';
import type { Owner, OwnerType } from '@/types/owners';

const ownerSchema = z
  .object({
    ownerType: z.union([z.literal(1), z.literal(2)]),
    firstName: z.string().trim().optional(),
    lastName: z.string().trim().optional(),
    nationalCode: z.string().trim().optional(),
    identityNumber: z.string().trim().optional(),
    mobile: z.string().trim().optional(),
    homePhone: z.string().trim().optional(),
    postalCode: z.string().trim().optional(),
    address: z.string().trim().optional(),
    companyName: z.string().trim().optional(),
    nationalId: z.string().trim().optional(),
    registrationNumber: z.string().trim().optional(),
    taxpayerId: z.string().trim().optional(),
  })
  .superRefine((values, context) => {
    if (values.ownerType === 1) {
      if (!values.firstName?.trim()) {
        context.addIssue({ code: z.ZodIssueCode.custom, path: ['firstName'], message: 'نام الزامی است.' });
      }
      if (!values.lastName?.trim()) {
        context.addIssue({ code: z.ZodIssueCode.custom, path: ['lastName'], message: 'نام خانوادگی الزامی است.' });
      }
      if (!values.nationalCode?.trim()) {
        context.addIssue({ code: z.ZodIssueCode.custom, path: ['nationalCode'], message: 'کد ملی الزامی است.' });
      } else if (!/^[0-9۰-۹٠-٩]{10}$/.test(values.nationalCode.trim())) {
        context.addIssue({ code: z.ZodIssueCode.custom, path: ['nationalCode'], message: 'کد ملی باید دقیقا 10 رقم باشد.' });
      }
    }

    if (values.ownerType === 2) {
      if (!values.companyName?.trim()) {
        context.addIssue({ code: z.ZodIssueCode.custom, path: ['companyName'], message: 'نام سازمان/شرکت الزامی است.' });
      }
      if (!values.nationalId?.trim()) {
        context.addIssue({ code: z.ZodIssueCode.custom, path: ['nationalId'], message: 'شناسه ملی الزامی است.' });
      } else if (!/^[0-9۰-۹٠-٩]{10,11}$/.test(values.nationalId.trim())) {
        context.addIssue({ code: z.ZodIssueCode.custom, path: ['nationalId'], message: 'شناسه ملی باید 10 یا 11 رقم باشد.' });
      }
    }

    if (values.taxpayerId?.trim() && !/^([0-9۰-۹٠-٩]{11}|[0-9۰-۹٠-٩]{14})$/.test(values.taxpayerId.trim())) {
      context.addIssue({ code: z.ZodIssueCode.custom, path: ['taxpayerId'], message: 'شناسه/کد مالیاتی باید 11 یا 14 رقم باشد.' });
    }

    if (values.postalCode?.trim() && !/^[0-9۰-۹٠-٩]{10}$/.test(values.postalCode.trim())) {
      context.addIssue({ code: z.ZodIssueCode.custom, path: ['postalCode'], message: 'کد پستی باید دقیقا 10 رقم باشد.' });
    }
  });

type CreateOwnerFormValues = z.infer<typeof ownerSchema>;

interface CreateOwnerModalProps {
  isOpen: boolean;
  onClose: () => void;
  mode?: 'create' | 'edit';
  owner?: Owner | null;
  onCreated?: (owner: Owner) => void;
  onUpdated?: (owner: Owner) => void;
}

const trimOrUndefined = (value?: string) => {
  if (!value) {
    return undefined;
  }

  const normalized = value.trim();
  return normalized.length > 0 ? normalized : undefined;
};

const emptyOwnerFormValues: CreateOwnerFormValues = {
  ownerType: 1,
  firstName: '',
  lastName: '',
  nationalCode: '',
  identityNumber: '',
  mobile: '',
  homePhone: '',
  postalCode: '',
  address: '',
  companyName: '',
  nationalId: '',
  registrationNumber: '',
  taxpayerId: '',
};

const mapOwnerToFormValues = (owner?: Owner | null): CreateOwnerFormValues => {
  if (!owner) {
    return emptyOwnerFormValues;
  }

  return {
    ownerType: owner.ownerType,
    firstName: owner.firstName ?? '',
    lastName: owner.lastName ?? '',
    nationalCode: owner.nationalCode ?? '',
    identityNumber: owner.identityNumber ?? '',
    mobile: owner.mobile ?? '',
    homePhone: owner.homePhone ?? '',
    postalCode: owner.postalCode ?? '',
    address: owner.address ?? '',
    companyName: owner.companyName ?? '',
    nationalId: owner.nationalId ?? '',
    registrationNumber: owner.registrationNumber ?? '',
    taxpayerId: owner.taxpayerId ?? '',
  };
};

export const CreateOwnerModal = ({ isOpen, onClose, mode = 'create', owner, onCreated, onUpdated }: CreateOwnerModalProps) => {
  const createOwnerMutation = useCreateOwner();
  const updateOwnerMutation = useUpdateOwner();
  const isEditMode = mode === 'edit';
  const isPending = createOwnerMutation.isPending || updateOwnerMutation.isPending;

  const {
    control,
    watch,
    handleSubmit,
    setValue,
    reset,
    formState: { errors },
  } = useForm<CreateOwnerFormValues>({
    resolver: zodResolver(ownerSchema),
    mode: 'onChange',
    defaultValues: emptyOwnerFormValues,
  });

  const ownerType = watch('ownerType');

  useEffect(() => {
    if (isOpen) {
      reset(mapOwnerToFormValues(owner));
      return;
    }

    reset(emptyOwnerFormValues);
  }, [isOpen, owner, reset]);

  const onSubmit = handleSubmit(async (values) => {
    const payload = {
      ownerType: values.ownerType as OwnerType,
      firstName: values.ownerType === 1 ? trimOrUndefined(values.firstName) : undefined,
      lastName: values.ownerType === 1 ? trimOrUndefined(values.lastName) : undefined,
      nationalCode: values.ownerType === 1 ? trimOrUndefined(values.nationalCode) : undefined,
      identityNumber: values.ownerType === 1 ? trimOrUndefined(values.identityNumber) : undefined,
      mobile: trimOrUndefined(values.mobile),
      homePhone: trimOrUndefined(values.homePhone),
      postalCode: trimOrUndefined(values.postalCode),
      address: trimOrUndefined(values.address),
      companyName: values.ownerType === 2 ? trimOrUndefined(values.companyName) : undefined,
      nationalId: values.ownerType === 2 ? trimOrUndefined(values.nationalId) : undefined,
      registrationNumber: values.ownerType === 2 ? trimOrUndefined(values.registrationNumber) : undefined,
      taxpayerId: trimOrUndefined(values.taxpayerId),
    };

    let saved: Owner;
    try {
      saved = isEditMode && owner
        ? await updateOwnerMutation.mutateAsync({ ownerId: owner.id, payload })
        : await createOwnerMutation.mutateAsync(payload);
    } catch {
      return;
    }

    if (isEditMode) {
      onUpdated?.(saved);
    } else {
      onCreated?.(saved);
    }
    onClose();
  });

  return (
    <Dialog
      open={isOpen}
      onClose={isPending ? undefined : onClose}
      maxWidth="md"
      fullWidth
      PaperProps={{
        sx: {
          direction: 'rtl',
          borderRadius: 3,
          overflow: 'hidden',
          border: '1px solid',
          borderColor: 'rgba(15, 23, 42, 0.08)',
          boxShadow: '0 24px 70px rgba(15, 23, 42, 0.18)',
        },
      }}
      slotProps={{
        backdrop: {
          sx: {
            backdropFilter: 'blur(6px)',
            backgroundColor: 'rgba(15, 23, 42, 0.32)',
          },
        },
      }}
    >
      <DialogTitle sx={{ px: 3, pt: 2.5, pb: 1.5, position: 'relative', borderBottom: '1px solid #e5e7eb' }}>
        <IconButton onClick={onClose} sx={{ position: 'absolute', left: 12, top: 12 }} aria-label="بستن" disabled={isPending}>
          <CloseIcon />
        </IconButton>
        <Typography variant="h6" sx={{ fontWeight: 700, color: 'text.primary' }}>
          {isEditMode ? 'ویرایش مالک' : 'ایجاد مودی جدید'}
        </Typography>
        <Typography variant="body2" sx={{ mt: 0.75, color: 'text.secondary' }}>
          لطفاً اطلاعات مودی را بر اساس نوع حقیقی یا حقوقی وارد کنید
        </Typography>
      </DialogTitle>

      <DialogContent sx={{ px: 3, py: 2.5 }}>
        <Box
          sx={{
            p: 0.5,
            borderRadius: 2,
            backgroundColor: 'action.hover',
            border: '1px solid #e2e8f0',
          }}
        >
          <Tabs
            value={ownerType}
            onChange={(_event, newValue: OwnerType) => setValue('ownerType', newValue, { shouldValidate: true })}
            variant="fullWidth"
            sx={{
              minHeight: 44,
              '& .MuiTabs-indicator': { display: 'none' },
            }}
          >
            <Tab icon={<PersonIcon fontSize="small" />} iconPosition="start" value={1} label="شخص حقیقی" sx={segmentedTabSx(ownerType === 1)} />
            <Tab icon={<CorporateFareIcon fontSize="small" />} iconPosition="start" value={2} label="شخص حقوقی" sx={segmentedTabSx(ownerType === 2)} />
          </Tabs>
        </Box>

        <Box component="form" id="create-owner-form" onSubmit={onSubmit} className="taxpayer-rtl-form mt-5 grid grid-cols-1 gap-4 md:grid-cols-2">
          {ownerType === 1 ? (
            <>
              <FormTextField control={control} name="firstName" label="نام" placeholder="مثال: علی" errorMessage={errors.firstName?.message} />
              <FormTextField control={control} name="lastName" label="نام خانوادگی" placeholder="مثال: رضایی" errorMessage={errors.lastName?.message} />
              <FormTextField control={control} name="nationalCode" label="کد ملی" placeholder="0012345678" inputMode="numeric" dir="ltr" errorMessage={errors.nationalCode?.message} />
              <FormTextField control={control} name="identityNumber" label="شماره شناسنامه" placeholder="123456" inputMode="numeric" dir="ltr" errorMessage={errors.identityNumber?.message} />
            </>
          ) : (
            <>
              <FormTextField control={control} name="companyName" label="نام سازمان/شرکت" placeholder="مثال: شرکت توسعه آرشیو" errorMessage={errors.companyName?.message} />
              <FormTextField control={control} name="nationalId" label="شناسه ملی" placeholder="10101234567" inputMode="numeric" dir="ltr" errorMessage={errors.nationalId?.message} />
              <FormTextField control={control} name="registrationNumber" label="شماره ثبت" placeholder="87654" inputMode="numeric" dir="ltr" errorMessage={errors.registrationNumber?.message} />
            </>
          )}

          <FormTextField control={control} name="mobile" label="شماره همراه" placeholder="09121234567" inputMode="tel" dir="ltr" errorMessage={errors.mobile?.message} />
          <FormTextField control={control} name="homePhone" label="تلفن ثابت" placeholder="02112345678" inputMode="tel" dir="ltr" errorMessage={errors.homePhone?.message} />
          <FormTextField control={control} name="taxpayerId" label="شناسه/کد مالیاتی" placeholder="اختیاری" inputMode="numeric" dir="ltr" errorMessage={errors.taxpayerId?.message} />
          <FormTextField control={control} name="postalCode" label="کد پستی" placeholder="1234567890" inputMode="numeric" dir="ltr" errorMessage={errors.postalCode?.message} />
          <Box className="md:col-span-2">
            <FormTextField control={control} name="address" label="آدرس" placeholder={ownerType === 1 ? 'آدرس کامل را وارد کنید' : 'آدرس کامل شرکت را وارد کنید'} multiline minRows={3} errorMessage={errors.address?.message} />
          </Box>
        </Box>

        <Box className="mt-6 border-t border-slate-200 pt-4">
          <Box sx={{ display: 'flex', justifyContent: 'flex-start', gap: 1.5 }}>
            <Button type="submit" form="create-owner-form" variant="contained" disabled={isPending} sx={{ minWidth: 130, fontWeight: 700 }}>
              {isPending ? (
                <Box sx={{ display: 'inline-flex', alignItems: 'center', gap: 1 }}>
                  <CircularProgress size={16} color="inherit" />
                  {isEditMode ? 'در حال ذخیره...' : 'در حال ثبت...'}
                </Box>
              ) : (
                isEditMode ? 'ذخیره تغییرات' : 'ثبت مودی'
              )}
            </Button>
            <Button onClick={onClose} variant="outlined" color="inherit" sx={{ minWidth: 110 }} disabled={isPending}>
              انصراف
            </Button>
          </Box>
        </Box>
      </DialogContent>
    </Dialog>
  );
};

const segmentedTabSx = (selected: boolean) => ({
  minHeight: 40,
  borderRadius: 1.5,
  textTransform: 'none',
  fontWeight: 700,
  color: selected ? 'text.primary' : 'text.secondary',
  backgroundColor: selected ? 'background.paper' : 'transparent',
  border: selected ? '1px solid #cbd5e1' : '1px solid transparent',
  boxShadow: selected ? '0 4px 14px rgba(15, 23, 42, 0.10)' : 'none',
});

interface FormTextFieldProps {
  control: Control<CreateOwnerFormValues>;
  name: keyof CreateOwnerFormValues;
  label: string;
  placeholder?: string;
  inputMode?: HTMLAttributes<HTMLInputElement>['inputMode'];
  dir?: 'rtl' | 'ltr';
  multiline?: boolean;
  minRows?: number;
  errorMessage?: string;
}

const FormTextField = ({
  control,
  name,
  label,
  placeholder,
  inputMode,
  dir,
  multiline,
  minRows,
  errorMessage,
}: FormTextFieldProps) => (
  <Controller
    control={control}
    name={name}
    render={({ field }) => (
      <TextField
        {...field}
        value={(field.value as string | undefined) ?? ''}
        fullWidth
        variant="outlined"
        label={label}
        placeholder={placeholder}
        inputMode={inputMode}
        inputProps={{
          dir: dir ?? 'rtl',
          style: {
            direction: dir ?? 'rtl',
            textAlign: dir === 'ltr' ? 'left' : 'right',
          },
        }}
        multiline={multiline}
        minRows={minRows}
        error={Boolean(errorMessage)}
        helperText={errorMessage ?? ' '}
        sx={{
          direction: dir ?? 'rtl',
          '& .MuiInputBase-input, & input, & textarea': {
            direction: `${dir ?? 'rtl'} !important`,
            textAlign: `${dir === 'ltr' ? 'left' : 'right'} !important`,
          },
        }}
        InputLabelProps={{
          shrink: true,
          sx: {
            right: 14,
            left: 'auto',
            px: 0.75,
            backgroundColor: 'background.paper',
            transformOrigin: 'top right',
            '&.MuiInputLabel-shrink': {
              transform: 'translate(0, -9px) scale(0.75)',
            },
          },
        }}
        InputProps={{
          sx: {
            direction: dir ?? 'rtl',
            borderRadius: 1.75,
            backgroundColor: 'background.paper',
            mt: 0.75,
            '& .MuiInputBase-input': {
              direction: `${dir ?? 'rtl'} !important`,
              textAlign: `${dir === 'ltr' ? 'left' : 'right'} !important`,
            },
          },
        }}
      />
    )}
  />
);
