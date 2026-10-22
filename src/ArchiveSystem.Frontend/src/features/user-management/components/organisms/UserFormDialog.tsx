import { zodResolver } from '@hookform/resolvers/zod';
import CloseIcon from '@mui/icons-material/Close';
import SaveIcon from '@mui/icons-material/Save';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import {
  Autocomplete,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { mapApiError } from '@/services/errorMapper';
import { createUserSchema, toCreatePayload, toUpdatePayload, updateUserSchema } from '@/features/user-management/schemas/userSchemas';
import { PasswordStrengthIndicator } from '@/features/user-management/components/atoms/PasswordStrengthIndicator';
import { usePersonnelSearchQuery } from '@/features/user-management/hooks/useUsersQuery';
import type { PersonnelOption, UserDTO, UserFormValues } from '@/features/user-management/types';

interface UserFormDialogProps {
  open: boolean;
  selectedUser: UserDTO | null;
  onClose: () => void;
  onCreate: (values: ReturnType<typeof toCreatePayload>) => Promise<void>;
  onUpdate: (values: ReturnType<typeof toUpdatePayload>) => Promise<void>;
  isSubmitting: boolean;
}

const defaultValues: UserFormValues = {
  personnelId: null,
  firstName: '',
  lastName: '',
  personnelCode: '',
  nationalId: '',
  email: '',
  phoneNumber: '',
  username: '',
  password: '',
  confirmPassword: '',
  activeDuration: '08:00:00',
  autoBackupEnabled: false,
  twoFactorEnabled: false,
};

const normalizeDuration = (value: string) => {
  const clean = value.replace(/[^\d]/g, '');
  const padded = clean.slice(0, 6).padEnd(6, '0');
  return `${padded.slice(0, 2)}:${padded.slice(2, 4)}:${padded.slice(4, 6)}`;
};

const rtlTextInputProps = {
  dir: 'rtl',
  style: { textAlign: 'right' },
} as const;

const formGridSx = {
  display: 'grid',
  gridTemplateColumns: { xs: '1fr', md: 'repeat(4, minmax(0, 1fr))' },
  gap: 1.25,
  alignItems: 'start',
} as const;

const fullWidthGridItemSx = {
  gridColumn: { xs: '1', md: '1 / -1' },
} as const;

export const UserFormDialog = ({ open, selectedUser, onClose, onCreate, onUpdate, isSubmitting }: UserFormDialogProps) => {
  const isEditMode = Boolean(selectedUser);

  const {
    control,
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    setError,
    formState: { errors },
  } = useForm<UserFormValues>({
    resolver: zodResolver(isEditMode ? updateUserSchema : createUserSchema),
    mode: 'onTouched',
    defaultValues,
  });

  const password = watch('password');
  const [personnelSearchTerm, setPersonnelSearchTerm] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  const personnelSearch = usePersonnelSearchQuery(personnelSearchTerm);

  useEffect(() => {
    if (!open) {
      reset(defaultValues);
      setPersonnelSearchTerm('');
      setShowPassword(false);
      return;
    }

    if (selectedUser) {
      reset({
        personnelId: null,
        firstName: selectedUser.firstName,
        lastName: selectedUser.lastName,
        personnelCode: selectedUser.personnelCode ?? '',
        nationalId: selectedUser.nationalId ?? '',
        email: selectedUser.email,
        phoneNumber: selectedUser.phoneNumber ?? '',
        username: selectedUser.username,
        password: '',
        confirmPassword: '',
        activeDuration: selectedUser.activeDuration?.slice(0, 8) || '08:00:00',
        autoBackupEnabled: selectedUser.autoBackupEnabled,
        twoFactorEnabled: selectedUser.twoFactorEnabled,
      });
    } else {
      reset(defaultValues);
    }
    setPersonnelSearchTerm('');
    setShowPassword(false);
  }, [open, reset, selectedUser]);

  const applyPersonnel = (option: PersonnelOption | null) => {
    if (!option) {
      return;
    }

    setValue('personnelId', option.id);
    setValue('firstName', option.firstName, { shouldValidate: true });
    setValue('lastName', option.lastName, { shouldValidate: true });
    setValue('personnelCode', option.personnelCode ?? '', { shouldValidate: true });
    setValue('nationalId', option.nationalId ?? '', { shouldValidate: true });
  };

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (isEditMode) {
        await onUpdate(toUpdatePayload(values));
      } else {
        await onCreate(toCreatePayload(values));
      }

      onClose();
    } catch (error) {
      const mapped = mapApiError(error);
      if (mapped.fieldErrors) {
        Object.entries(mapped.fieldErrors).forEach(([field, messages]) => {
          const firstMessage = messages[0] ?? 'مقدار نامعتبر است.';
          const normalizedField = field.toLowerCase();
          if (normalizedField.includes('username')) {
            setError('username', { message: firstMessage });
          } else if (normalizedField.includes('email')) {
            setError('email', { message: firstMessage });
          } else if (normalizedField.includes('phone')) {
            setError('phoneNumber', { message: firstMessage });
          } else if (normalizedField.includes('password')) {
            setError('password', { message: firstMessage });
          } else if (normalizedField.includes('nationalid')) {
            setError('nationalId', { message: firstMessage });
          } else if (normalizedField.includes('personnelcode')) {
            setError('personnelCode', { message: firstMessage });
          }
        });
      } else if (mapped.status === 409) {
        const detail = (mapped.details ?? mapped.message ?? '').toLowerCase();
        if (detail.includes('username')) {
          setError('username', { message: mapped.message || 'این نام کاربری قبلاً استفاده شده است.' });
        } else if (detail.includes('email')) {
          setError('email', { message: mapped.message || 'این ایمیل قبلاً استفاده شده است.' });
        } else if (detail.includes('شماره همراه') || detail.includes('phone')) {
          setError('phoneNumber', { message: mapped.message || 'این شماره همراه قبلاً ثبت شده است.' });
        } else if (detail.includes('nationalid')) {
          setError('nationalId', { message: mapped.message || 'این کد ملی قبلاً استفاده شده است.' });
        } else if (detail.includes('personnelcode')) {
          setError('personnelCode', { message: mapped.message || 'این کد پرسنلی قبلاً استفاده شده است.' });
        }
      }
    }
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      fullWidth
      maxWidth="lg"
      PaperProps={{
        dir: 'rtl',
        sx: { direction: 'rtl', borderRadius: 2, maxWidth: 1100 },
      }}
      aria-labelledby="user-form-dialog-title"
    >
      <DialogTitle id="user-form-dialog-title" sx={{ position: 'relative', py: 1.25 }}>
        <IconButton onClick={onClose} aria-label="بستن فرم کاربر" sx={{ position: 'absolute', left: 12, top: 12 }}>
          <CloseIcon />
        </IconButton>
        <Typography variant="h6" fontWeight={700}>
          {isEditMode ? 'ویرایش کاربر' : 'ایجاد کاربر جدید'}
        </Typography>
      </DialogTitle>

      <DialogContent dividers sx={{ p: 2 }}>
        <Box component="form" onSubmit={onSubmit} id="user-form" dir="rtl">
          <Stack spacing={1.25}>
            <Box sx={formGridSx}>
              <Typography variant="subtitle2" fontWeight={700} sx={fullWidthGridItemSx}>
                اطلاعات پرسنلی
              </Typography>
              {/* <Box sx={fullWidthGridItemSx}>
                <Controller
                  name="personnelId"
                  control={control}
                  render={({ field }) => (
                    <Autocomplete<PersonnelOption, false, false, false>
                      options={personnelSearch.data ?? []}
                      loading={personnelSearch.isFetching}
                      filterOptions={(options) => options}
                      getOptionLabel={(option) => option.label}
                      noOptionsText={personnelSearchTerm.trim().length < 2 ? 'حداقل 2 کاراکتر وارد کنید.' : 'نتیجه‌ای یافت نشد.'}
                      value={personnelSearch.data?.find((x) => x.id === field.value) ?? null}
                      inputValue={personnelSearchTerm}
                      onInputChange={(_event, value) => setPersonnelSearchTerm(value)}
                      onChange={(_event, option) => {
                        field.onChange(option?.id ?? null);
                        applyPersonnel(option);
                      }}
                      renderInput={(params) => (
                        <TextField
                          {...params}
                          size="small"
                          label="جستجوی پرسنل"
                          placeholder="نام یا کد پرسنلی را وارد کنید"
                          aria-label="جستجوی پرسنل"
                          inputProps={{
                            ...params.inputProps,
                            ...rtlTextInputProps,
                          }}
                          InputProps={{
                            ...params.InputProps,
                            endAdornment: (
                              <>
                                {personnelSearch.isFetching ? <CircularProgress size={18} /> : null}
                                {params.InputProps.endAdornment}
                              </>
                            ),
                          }}
                        />
                      )}
                    />
                  )}
                />
              </Box> */}
              <TextField
                {...register('firstName')}
                size="small"
                label="نام"
                error={Boolean(errors.firstName)}
                helperText={errors.firstName?.message}
                fullWidth
                inputProps={{ ...rtlTextInputProps, 'aria-label': 'نام' }}
              />
              <TextField
                {...register('lastName')}
                size="small"
                label="نام خانوادگی"
                error={Boolean(errors.lastName)}
                helperText={errors.lastName?.message}
                fullWidth
                inputProps={{ ...rtlTextInputProps, 'aria-label': 'نام خانوادگی' }}
              />
              <TextField
                {...register('personnelCode')}
                size="small"
                label="کد پرسنلی"
                error={Boolean(errors.personnelCode)}
                helperText={errors.personnelCode?.message}
                fullWidth
                inputProps={{ dir: 'ltr', 'aria-label': 'کد پرسنلی' }}
              />
              <TextField
                {...register('nationalId')}
                size="small"
                label="کد ملی"
                error={Boolean(errors.nationalId)}
                helperText={errors.nationalId?.message}
                fullWidth
                inputProps={{ inputMode: 'numeric', dir: 'ltr', 'aria-label': 'کد ملی' }}
              />
            </Box>

            <Box sx={formGridSx}>
              <Typography variant="subtitle2" fontWeight={700} sx={fullWidthGridItemSx}>
                امنیت
              </Typography>
              <TextField
                {...register('email')}
                size="small"
                label="ایمیل"
                error={Boolean(errors.email)}
                helperText={errors.email?.message}
                fullWidth
                inputProps={{ dir: 'ltr', 'aria-label': 'ایمیل' }}
              />
              <TextField
                {...register('phoneNumber')}
                size="small"
                label="شماره همراه"
                error={Boolean(errors.phoneNumber)}
                helperText={errors.phoneNumber?.message}
                fullWidth
                inputProps={{ inputMode: 'tel', dir: 'ltr', 'aria-label': 'شماره همراه' }}
              />
              <TextField
                {...register('username')}
                size="small"
                label="نام کاربری"
                error={Boolean(errors.username)}
                helperText={errors.username?.message}
                fullWidth
                inputProps={{ dir: 'ltr', 'aria-label': 'نام کاربری' }}
              />

              {!isEditMode && (
                <>
                  <TextField
                    {...register('password')}
                    size="small"
                    type={showPassword ? 'text' : 'password'}
                    label="رمز عبور"
                    error={Boolean(errors.password)}
                    helperText={errors.password?.message}
                    fullWidth
                    inputProps={{ dir: 'ltr', 'aria-label': 'رمز عبور' }}
                    InputProps={{
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            aria-label={showPassword ? 'مخفی کردن رمز عبور' : 'نمایش رمز عبور'}
                            edge="end"
                            size="small"
                            onClick={() => setShowPassword((value) => !value)}
                            onMouseDown={(event) => event.preventDefault()}
                          >
                            {showPassword ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    }}
                  />
                  <TextField
                    {...register('confirmPassword')}
                    size="small"
                    type={showPassword ? 'text' : 'password'}
                    label="تکرار رمز عبور"
                    error={Boolean(errors.confirmPassword)}
                    helperText={errors.confirmPassword?.message}
                    fullWidth
                    inputProps={{ dir: 'ltr', 'aria-label': 'تکرار رمز عبور' }}
                    InputProps={{
                      endAdornment: (
                        <InputAdornment position="end">
                          <IconButton
                            aria-label={showPassword ? 'مخفی کردن تکرار رمز عبور' : 'نمایش تکرار رمز عبور'}
                            edge="end"
                            size="small"
                            onClick={() => setShowPassword((value) => !value)}
                            onMouseDown={(event) => event.preventDefault()}
                          >
                            {showPassword ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                          </IconButton>
                        </InputAdornment>
                      ),
                    }}
                  />
                  <Box sx={fullWidthGridItemSx}>
                    <PasswordStrengthIndicator password={password} />
                  </Box>
                </>
              )}
            </Box>

            <Box sx={formGridSx}>
              <Typography variant="subtitle2" fontWeight={700} sx={fullWidthGridItemSx}>
                تنظیمات
              </Typography>
              <TextField
                {...register('activeDuration')}
                size="small"
                label="مدت فعال بودن (HH:mm:ss)"
                error={Boolean(errors.activeDuration)}
                helperText={errors.activeDuration?.message ?? 'مثال: 08:30:00'}
                fullWidth
                inputProps={{ dir: 'ltr', 'aria-label': 'مدت فعال بودن', maxLength: 8 }}
                onChange={(event) => {
                  const masked = normalizeDuration(event.target.value);
                  setValue('activeDuration', masked, { shouldValidate: true });
                }}
              />

              {/* <Controller
                name="autoBackupEnabled"
                control={control}
                render={({ field }) => (
                  <FormControlLabel
                    control={<Switch checked={field.value} onChange={(_, checked) => field.onChange(checked)} />}
                    label="عضویت کاربر در سیاست پشتیبان‌گیری خودکار"
                  />
                )}
              /> */}

              <Controller
                name="twoFactorEnabled"
                control={control}
                render={({ field }) => (
                  <FormControlLabel
                    sx={{ m: 0, minHeight: 40 }}
                    control={<Switch checked={field.value} onChange={(_, checked) => field.onChange(checked)} />}
                    label="فعال‌سازی ورود با کد پیامکی (2FA)"
                  />
                )}
              />
            </Box>
          </Stack>
        </Box>
      </DialogContent>

      <DialogActions sx={{ px: 2.5, py: 1 }}>
        <Button onClick={onClose} disabled={isSubmitting}>
          انصراف
        </Button>
        <Button
          type="submit"
          form="user-form"
          variant="contained"
          disabled={isSubmitting}
          startIcon={isSubmitting ? <CircularProgress size={16} color="inherit" /> : <SaveIcon />}
          aria-label={isEditMode ? 'ذخیره تغییرات کاربر' : 'ایجاد کاربر'}
        >
          {isEditMode ? 'ذخیره تغییرات' : 'ایجاد کاربر'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};
