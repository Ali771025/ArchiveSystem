import { zodResolver } from '@hookform/resolvers/zod';
import AdminPanelSettingsRoundedIcon from '@mui/icons-material/AdminPanelSettingsRounded';
import PersonRoundedIcon from '@mui/icons-material/PersonRounded';
import PhoneAndroidRoundedIcon from '@mui/icons-material/PhoneAndroidRounded';
import LockRoundedIcon from '@mui/icons-material/LockRounded';
import VisibilityRoundedIcon from '@mui/icons-material/VisibilityRounded';
import VisibilityOffRoundedIcon from '@mui/icons-material/VisibilityOffRounded';
import {
  Alert,
  Box,
  Button,
  Divider,
  IconButton,
  InputAdornment,
  Link,
  Paper,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { EnterMobileForm } from '@/features/auth/components/EnterMobileForm';
import { VerifyOtpForm } from '@/features/auth/components/VerifyOtpForm';
import {
  UsernamePasswordSchema,
  usernamePasswordSchema,
} from '@/features/auth/schemas/loginSchemas';
import {
  mapAuthErrorMessage,
  usePasswordLoginMutation,
  VerifyOtpAndLoginResponse,
} from '@/features/auth/hooks/useOtpAuthMutations';
import { useOtpFlowStore } from '@/features/auth/store/useOtpFlowStore';
import { resolveRolePermissions } from '@/services/rolePermissions';
import { useSessionStore } from '@/stores/useSessionStore';
import { useToastStore } from '@/stores/useToastStore';
import type { SessionUser } from '@/types/archive';
import './Login.css';

type LoginMode = 'password' | 'mobile';

const parseJwtPayload = (token: string): Record<string, unknown> | null => {
  try {
    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }

    const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = payload.padEnd(payload.length + ((4 - (payload.length % 4)) % 4), '=');
    const decoded = atob(padded);

    const bytes = Uint8Array.from(decoded, (char) => char.charCodeAt(0));
    const jsonText = new TextDecoder().decode(bytes);
    return JSON.parse(jsonText) as Record<string, unknown>;
  } catch {
    return null;
  }
};

const getRolesFromPayload = (payload: Record<string, unknown>): string[] => {
  const roleValue =
    payload.role ?? payload.roles ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

  const normalizedRoles = Array.isArray(roleValue)
    ? roleValue
        .map((value) => String(value).trim())
        .filter((value) => value.length > 0)
    : typeof roleValue === 'string' && roleValue.trim()
      ? [roleValue.trim()]
      : [];

  return Array.from(new Set(normalizedRoles));
};

const getPermissionsFromPayload = (payload: Record<string, unknown>): SessionUser['permissions'] => {
  const permissionValue = payload.permission ?? payload.permissions;
  const values = Array.isArray(permissionValue)
    ? permissionValue
    : typeof permissionValue === 'string'
      ? [permissionValue]
      : [];

  return Array.from(new Set(values.map((value) => String(value).trim()).filter(Boolean))) as SessionUser['permissions'];
};

const buildSessionUserFromOtp = (mobileNumber: string, token: string): SessionUser => {
  const payload = parseJwtPayload(token) ?? {};

  const userIdRaw =
    payload.sub ?? payload.nameid ?? payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
  const usernameRaw = payload.unique_name ?? payload.name ?? mobileNumber;
  const emailRaw = payload.email ?? `${mobileNumber}@archive.local`;
  const firstNameRaw =
    payload.first_name ??
    payload.given_name ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'];
  const lastNameRaw =
    payload.last_name ??
    payload.family_name ??
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'];
  const personnelCodeRaw = payload.personnel_code;

  const roles = getRolesFromPayload(payload);
  const tokenPermissions = getPermissionsFromPayload(payload);
  const permissions = tokenPermissions.length > 0 ? tokenPermissions : resolveRolePermissions(roles);

  return {
    id: Number(userIdRaw) || 0,
    username: String(usernameRaw),
    email: String(emailRaw),
    firstName: String(firstNameRaw ?? usernameRaw),
    lastName: String(lastNameRaw ?? ''),
    personnelCode: personnelCodeRaw ? String(personnelCodeRaw) : undefined,
    roles,
    permissions,
  };
};

export const Login = () => {
  const navigate = useNavigate();
  const setSession = useSessionStore((state) => state.setSession);
  const pushToast = useToastStore((state) => state.push);

  const loginMutation = usePasswordLoginMutation();

  const [mode, setMode] = useState<LoginMode>(() =>
    useOtpFlowStore.getState().step === 'OTP_VERIFY' ? 'mobile' : 'password',
  );
  const [showPassword, setShowPassword] = useState(false);
  const otpFlowStep = useOtpFlowStore((state) => state.step);
  const otpPhoneNumber = useOtpFlowStore((state) => state.phoneNumber);
  const otpExpiresAtEpochMs = useOtpFlowStore((state) => state.otpExpiresAtEpochMs);
  const setOtpPhoneInput = useOtpFlowStore((state) => state.setPhoneInput);
  const moveToOtpVerify = useOtpFlowStore((state) => state.moveToOtpVerify);
  const restartOtpFlow = useOtpFlowStore((state) => state.restartFlow);
  const clearOtpFlow = useOtpFlowStore((state) => state.clearFlow);

  const {
    register,
    handleSubmit,
    clearErrors,
    formState: { errors },
  } = useForm<UsernamePasswordSchema>({
    resolver: zodResolver(usernamePasswordSchema),
    defaultValues: {
      username: '',
      password: '',
    },
  });

  const onPasswordSubmit = handleSubmit(async (values) => {
    try {
      const result = await loginMutation.mutateAsync(values);
      if (result.requiresTwoFactor) {
        const parsedExpiry = result.otpExpiresAtUtc
          ? Date.parse(result.otpExpiresAtUtc)
          : Number.NaN;
        const otpExpiresAtEpochMs = Number.isFinite(parsedExpiry)
          ? parsedExpiry
          : Date.now() + 120_000;
        const requestId =
          typeof crypto !== 'undefined' && 'randomUUID' in crypto
            ? crypto.randomUUID()
            : `${Date.now()}-${Math.random().toString(16).slice(2)}`;

        setOtpPhoneInput(result.mobileNumber);
        moveToOtpVerify({ requestId, otpExpiresAtEpochMs });
        setMode('mobile');
        pushToast('کد ورود دومرحله‌ای به شماره همراه شما ارسال شد.', 'info');
        return;
      }

      setSession(result.token, result.user);
      pushToast('ورود با موفقیت انجام شد.', 'success');
      navigate('/');
    } catch (error) {
      pushToast(mapAuthErrorMessage(error), 'error');
    }
  });

  const handleOtpRequested = (normalizedMobile: string, expiresAtEpochMs: number) => {
    const requestId =
      typeof crypto !== 'undefined' && 'randomUUID' in crypto
        ? crypto.randomUUID()
        : `${Date.now()}-${Math.random().toString(16).slice(2)}`;

    setOtpPhoneInput(normalizedMobile);
    moveToOtpVerify({ requestId, otpExpiresAtEpochMs: expiresAtEpochMs });
  };

  const handleOtpVerified = (result: VerifyOtpAndLoginResponse) => {
    if (!result.accessToken) {
      pushToast('توکن ورود دریافت نشد.', 'error');
      return;
    }

    const sessionUser = buildSessionUserFromOtp(otpPhoneNumber, result.accessToken);
    setSession(result.accessToken, sessionUser);
    clearOtpFlow();
    pushToast('ورود با کد تایید با موفقیت انجام شد.', 'success');
    navigate('/');
  };

  return (
    <Paper className="auth-login-root" dir="rtl" variant="outlined">
      <Stack className="auth-login-content" spacing={0}>
        <Box className="auth-login-header">
          <Box>
            <Typography variant="h6" className="auth-login-title">ورود کاربران</Typography>
            <Typography variant="body2" color="text.secondary">احراز هویت برای دسترسی به سامانه</Typography>
          </Box>
          <Box className="auth-login-badge" aria-hidden="true">
            <AdminPanelSettingsRoundedIcon fontSize="small" />
          </Box>
        </Box>

        <Tabs
          className="auth-mode-tabs"
          value={mode}
          onChange={(_e, value: LoginMode) => {
            setMode(value);
            if (value === 'password') {
              restartOtpFlow();
            }
            clearErrors();
          }}
          variant="fullWidth"
        >
          <Tab
            value="password"
            icon={<PersonRoundedIcon fontSize="small" />}
            iconPosition="start"
            label="ورود با نام کاربری"
          />
          <Tab
            value="mobile"
            icon={<PhoneAndroidRoundedIcon fontSize="small" />}
            iconPosition="start"
            label="ورود با شماره موبایل"
          />
        </Tabs>

        {mode === 'password' ? (
          <Stack className="auth-form-stack" component="form" spacing={1.5} onSubmit={onPasswordSubmit} noValidate>
            <TextField
              label="نام کاربری"
              autoComplete="username"
              size="small"
              {...register('username')}
              error={Boolean(errors.username)}
              helperText={errors.username?.message}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <PersonRoundedIcon fontSize="small" />
                  </InputAdornment>
                ),
              }}
            />

            <TextField
              label="گذرواژه"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              size="small"
              {...register('password')}
              error={Boolean(errors.password)}
              helperText={errors.password?.message}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <LockRoundedIcon fontSize="small" />
                  </InputAdornment>
                ),
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton
                      edge="end"
                      onClick={() => setShowPassword((current) => !current)}
                      aria-label={showPassword ? 'مخفی کردن گذرواژه' : 'نمایش گذرواژه'}
                    >
                      {showPassword ? <VisibilityOffRoundedIcon fontSize="small" /> : <VisibilityRoundedIcon fontSize="small" />}
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />

            <Box className="auth-password-actions">
              <Link component={RouterLink} to="/forgot-password" underline="hover">
                رمز عبور خود را فراموش کرده‌اید؟
              </Link>
            </Box>

            {loginMutation.isError ? <Alert severity="error">{mapAuthErrorMessage(loginMutation.error)}</Alert> : null}

            <Button className="auth-submit-button" variant="contained" type="submit" disabled={loginMutation.isPending} fullWidth>
              {loginMutation.isPending ? 'در حال ورود...' : 'ورود'}
            </Button>
          </Stack>
        ) : otpFlowStep !== 'OTP_VERIFY' || !otpPhoneNumber ? (
          <Box className="auth-step-panel">
            <Stack spacing={1.5}>
              <Box className="auth-step-header">
                <Typography variant="caption" fontWeight={800}>مرحله ۱ از ۲</Typography>
                <Typography variant="caption" color="text.secondary">ارسال کد تایید</Typography>
              </Box>
              <Divider />
              <EnterMobileForm onSuccess={handleOtpRequested} />
            </Stack>
          </Box>
        ) : (
          <Box className="auth-step-panel auth-step-panel-active">
            <Stack spacing={1.5}>
              <Box className="auth-step-header">
                <Typography variant="caption" fontWeight={800} color="primary.main">مرحله ۲ از ۲</Typography>
                <Typography variant="caption" color="text.secondary">تایید کد</Typography>
              </Box>
              <Divider />
              <VerifyOtpForm
                mobileNumber={otpPhoneNumber}
                initialExpiresAtEpochMs={otpExpiresAtEpochMs}
                onResendSucceeded={(expiresAtEpochMs) => {
                  moveToOtpVerify({
                    requestId:
                      typeof crypto !== 'undefined' && 'randomUUID' in crypto
                        ? crypto.randomUUID()
                        : `${Date.now()}-${Math.random().toString(16).slice(2)}`,
                    otpExpiresAtEpochMs: expiresAtEpochMs,
                  });
                }}
                onBack={restartOtpFlow}
                onVerified={handleOtpVerified}
              />
            </Stack>
          </Box>
        )}
      </Stack>
    </Paper>
  );
};
