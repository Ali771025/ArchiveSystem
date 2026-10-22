import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded';
import KeyRoundedIcon from '@mui/icons-material/KeyRounded';
import LockRoundedIcon from '@mui/icons-material/LockRounded';
import PhoneAndroidRoundedIcon from '@mui/icons-material/PhoneAndroidRounded';
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  InputAdornment,
  Link,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { OtpInput } from '@/features/auth/components/OtpInput';
import {
  mapAuthErrorMessage,
  useRequestPasswordResetOtpMutation,
  useResetPasswordWithOtpMutation,
} from '@/features/auth/hooks/useOtpAuthMutations';
import {
  EnterMobileSchema,
  enterMobileSchema,
  normalizeIranianMobile,
} from '@/features/auth/schemas/loginSchemas';
import {
  ResetPasswordSchema,
  resetPasswordSchema,
} from '@/features/auth/schemas/authSchemas';
import { useToastStore } from '@/stores/useToastStore';
import '@/features/auth/components/Login.css';

type ForgotPasswordStep = 'mobile' | 'otp' | 'done';

const OTP_LENGTH = 5;

export const ForgotPasswordPage = () => {
  const navigate = useNavigate();
  const pushToast = useToastStore((state) => state.push);
  const requestOtpMutation = useRequestPasswordResetOtpMutation();
  const resetPasswordMutation = useResetPasswordWithOtpMutation();

  const [step, setStep] = useState<ForgotPasswordStep>('mobile');
  const [mobileNumber, setMobileNumber] = useState('');
  const [otpCode, setOtpCode] = useState('');

  const mobileForm = useForm<EnterMobileSchema>({
    resolver: zodResolver(enterMobileSchema),
    defaultValues: {
      mobileNumber: '',
    },
  });

  const passwordForm = useForm<ResetPasswordSchema>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      newPassword: '',
      confirmPassword: '',
    },
  });

  const submitMobile = mobileForm.handleSubmit(async (values) => {
    try {
      const normalizedMobile = normalizeIranianMobile(values.mobileNumber);
      const result = await requestOtpMutation.mutateAsync({ mobileNumber: normalizedMobile });
      if (!result.succeeded) {
        throw new Error(result.message || 'ارسال کد بازیابی انجام نشد.');
      }

      setMobileNumber(normalizedMobile);
      setOtpCode('');
      setStep('otp');
      pushToast(result.message || 'کد بازیابی ارسال شد.', 'success');
    } catch (error) {
      pushToast(mapAuthErrorMessage(error), 'error');
    }
  });

  const submitPassword = passwordForm.handleSubmit(async (values) => {
    if (otpCode.length !== OTP_LENGTH) {
      pushToast('کد تایید باید ۵ رقم باشد.', 'error');
      return;
    }

    try {
      const result = await resetPasswordMutation.mutateAsync({
        mobileNumber,
        otpCode,
        newPassword: values.newPassword,
      });

      setStep('done');
      pushToast(result.message || 'گذرواژه با موفقیت تغییر کرد.', 'success');
    } catch (error) {
      pushToast(mapAuthErrorMessage(error), 'error');
    }
  });

  return (
    <div className="auth-page-shell" dir="rtl">
      <div className="auth-page-frame">
        <header className="auth-page-brand" aria-label="بازیابی گذرواژه">
          <div className="auth-brand-mark">
            <KeyRoundedIcon />
          </div>
          <p className="auth-kicker">سامانه سازمانی مدیریت اسناد</p>
          <h1 className="auth-page-title">بازیابی گذرواژه</h1>
        </header>

        <section className="auth-page-card" aria-label="فرم بازیابی گذرواژه">
          <Paper className="auth-login-root" dir="rtl" variant="outlined">
            <Stack className="auth-login-content" spacing={0}>
              <Box className="auth-login-header">
                <Box className="auth-login-badge" aria-hidden="true">
                  <LockRoundedIcon fontSize="small" />
                </Box>
                <Box>
                  <Typography variant="h6" className="auth-login-title">
                    تنظیم گذرواژه جدید
                  </Typography>
                  <Typography variant="body2">
                    کد تایید به شماره موبایل ثبت‌شده در حساب کاربری ارسال می‌شود.
                  </Typography>
                </Box>
              </Box>

              {step === 'mobile' ? (
                <Stack className="auth-form-stack" component="form" spacing={1.5} onSubmit={submitMobile} noValidate>
                  <TextField
                    label="شماره موبایل"
                    dir="ltr"
                    autoComplete="tel"
                    inputProps={{ inputMode: 'tel' }}
                    placeholder="09123456789"
                    size="small"
                    {...mobileForm.register('mobileNumber')}
                    error={Boolean(mobileForm.formState.errors.mobileNumber)}
                    helperText={mobileForm.formState.errors.mobileNumber?.message}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <PhoneAndroidRoundedIcon fontSize="small" />
                        </InputAdornment>
                      ),
                    }}
                  />

                  {requestOtpMutation.isError ? (
                    <Alert severity="error">{mapAuthErrorMessage(requestOtpMutation.error)}</Alert>
                  ) : null}

                  <Button type="submit" variant="contained" disabled={requestOtpMutation.isPending} fullWidth>
                    {requestOtpMutation.isPending ? <CircularProgress size={20} color="inherit" /> : 'ارسال کد تایید'}
                  </Button>

                  <Typography variant="body2" color="text.secondary" textAlign="center">
                    بازگشت به{' '}
                    <Link component={RouterLink} to="/login" underline="hover">
                      صفحه ورود
                    </Link>
                  </Typography>
                </Stack>
              ) : null}

              {step === 'otp' ? (
                <Stack className="auth-form-stack" component="form" spacing={1.5} onSubmit={submitPassword} noValidate>
                  <Box className="auth-step-header">
                    <Typography variant="caption" fontWeight={800} color="primary.main">
                      کد تایید ۵ رقمی
                    </Typography>
                    <IconButton
                      size="small"
                      onClick={() => {
                        setStep('mobile');
                        setOtpCode('');
                      }}
                      aria-label="تغییر شماره موبایل"
                    >
                      <ArrowBackRoundedIcon fontSize="small" />
                    </IconButton>
                  </Box>

                  <Typography variant="body2" color="text.secondary">
                    کد بازیابی برای شماره <bdi style={{ fontWeight: 700 }}>{mobileNumber}</bdi> ارسال شد.
                  </Typography>

                  <OtpInput length={OTP_LENGTH} value={otpCode} onChange={setOtpCode} disabled={resetPasswordMutation.isPending} autoFocus />

                  <TextField
                    label="گذرواژه جدید"
                    type="password"
                    autoComplete="new-password"
                    size="small"
                    {...passwordForm.register('newPassword')}
                    error={Boolean(passwordForm.formState.errors.newPassword)}
                    helperText={passwordForm.formState.errors.newPassword?.message}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <LockRoundedIcon fontSize="small" />
                        </InputAdornment>
                      ),
                    }}
                  />

                  <TextField
                    label="تکرار گذرواژه جدید"
                    type="password"
                    autoComplete="new-password"
                    size="small"
                    {...passwordForm.register('confirmPassword')}
                    error={Boolean(passwordForm.formState.errors.confirmPassword)}
                    helperText={passwordForm.formState.errors.confirmPassword?.message}
                    InputProps={{
                      startAdornment: (
                        <InputAdornment position="start">
                          <LockRoundedIcon fontSize="small" />
                        </InputAdornment>
                      ),
                    }}
                  />

                  {resetPasswordMutation.isError ? (
                    <Alert severity="error">{mapAuthErrorMessage(resetPasswordMutation.error)}</Alert>
                  ) : null}

                  <Button
                    type="submit"
                    variant="contained"
                    disabled={resetPasswordMutation.isPending || otpCode.length !== OTP_LENGTH}
                    fullWidth
                  >
                    {resetPasswordMutation.isPending ? <CircularProgress size={20} color="inherit" /> : 'ثبت گذرواژه جدید'}
                  </Button>
                </Stack>
              ) : null}

              {step === 'done' ? (
                <Stack className="auth-form-stack" spacing={1.5}>
                  <Alert severity="success">گذرواژه با موفقیت تغییر کرد. اکنون می‌توانید وارد سامانه شوید.</Alert>
                  <Button variant="contained" onClick={() => navigate('/login')} fullWidth>
                    ورود به سامانه
                  </Button>
                </Stack>
              ) : null}
            </Stack>
          </Paper>
        </section>
      </div>
    </div>
  );
};
