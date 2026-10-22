/* @vitest-environment jsdom */
import '@testing-library/jest-dom/vitest';
import { ThemeProvider } from '@mui/material/styles';
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Login } from '@/features/auth/components/Login';
import { useOtpFlowStore } from '@/features/auth/store/useOtpFlowStore';
import { queryClient } from '@/services/queryClient';
import { useSessionStore } from '@/stores/useSessionStore';
import { appTheme } from '@/theme';

const passwordLoginMutateAsync = vi.fn();
const requestOtpMutateAsync = vi.fn();
const verifyOtpMutateAsync = vi.fn();

vi.mock('@/features/auth/hooks/useOtpAuthMutations', () => ({
  mapAuthErrorMessage: () => 'خطا در ورود',
  usePasswordLoginMutation: () => ({
    isPending: false,
    isError: false,
    error: new Error('login failed'),
    mutateAsync: passwordLoginMutateAsync,
  }),
  useRequestOtpMutation: () => ({
    isPending: false,
    isError: false,
    error: null,
    mutateAsync: requestOtpMutateAsync,
  }),
  useVerifyOtpAndLoginMutation: () => ({
    isPending: false,
    isError: false,
    error: null,
    mutateAsync: verifyOtpMutateAsync,
  }),
}));

const renderLogin = () => {
  return render(
    <MemoryRouter>
      <ThemeProvider theme={appTheme}>
        <Login />
      </ThemeProvider>
    </MemoryRouter>,
  );
};

describe('Login security validation', () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    useSessionStore.getState().clearSession();
    useOtpFlowStore.getState().clearFlow();
    passwordLoginMutateAsync.mockReset();
    requestOtpMutateAsync.mockReset();
    verifyOtpMutateAsync.mockReset();
    passwordLoginMutateAsync.mockRejectedValue(new Error('invalid credentials'));
  });

  afterEach(() => {
    cleanup();
    vi.restoreAllMocks();
  });

  it('shows required validation errors when login fields are empty', async () => {
    const user = userEvent.setup();
    renderLogin();

    await user.click(screen.getByRole('button', { name: 'ورود' }));

    expect(await screen.findByText('نام کاربری الزامی است.')).toBeInTheDocument();
    expect(screen.getByText('گذرواژه الزامی است.')).toBeInTheDocument();
    expect(passwordLoginMutateAsync).not.toHaveBeenCalled();
  });

  it('rejects username that only contains spaces', async () => {
    const user = userEvent.setup();
    renderLogin();

    await user.type(screen.getByLabelText('نام کاربری'), '   ');
    await user.type(screen.getByLabelText('گذرواژه'), 'CorrectPassword');
    await user.click(screen.getByRole('button', { name: 'ورود' }));

    expect(await screen.findByText('نام کاربری الزامی است.')).toBeInTheDocument();
    expect(passwordLoginMutateAsync).not.toHaveBeenCalled();
  });

  it('submits SQL injection text as plain input without client-side bypass', async () => {
    const user = userEvent.setup();
    renderLogin();

    await user.type(screen.getByLabelText('نام کاربری'), "' OR 1=1 --");
    await user.type(screen.getByLabelText('گذرواژه'), 'CorrectPassword');
    await user.click(screen.getByRole('button', { name: 'ورود' }));

    await waitFor(() => {
      expect(passwordLoginMutateAsync).toHaveBeenCalledWith({
        username: "' OR 1=1 --",
        password: 'CorrectPassword',
      });
    });
  });

  it('does not execute script text entered into login fields', async () => {
    const user = userEvent.setup();
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => undefined);
    renderLogin();

    await user.type(screen.getByLabelText('نام کاربری'), '<script>alert(1)</script>');
    await user.type(screen.getByLabelText('گذرواژه'), 'CorrectPassword');
    await user.click(screen.getByRole('button', { name: 'ورود' }));

    await waitFor(() => {
      expect(passwordLoginMutateAsync).toHaveBeenCalled();
    });
    expect(alertSpy).not.toHaveBeenCalled();
  });

  it('requires OTP after a valid password when 2FA is enabled and does not create a session', async () => {
    passwordLoginMutateAsync.mockResolvedValue({
      requiresTwoFactor: true,
      mobileNumber: '09121234567',
      otpExpiresAtUtc: new Date(Date.now() + 120_000).toISOString(),
    });
    const user = userEvent.setup();
    renderLogin();

    await user.type(screen.getByLabelText('نام کاربری'), 'two-factor-user');
    await user.type(screen.getByLabelText('گذرواژه'), 'CorrectPassword');
    await user.click(screen.getByRole('button', { name: 'ورود' }));

    expect(await screen.findByText('کد تایید ۵ رقمی')).toBeInTheDocument();
    expect(screen.getByText(/0912\*{6}67/)).toBeInTheDocument();
    expect(useSessionStore.getState().isAuthenticated).toBe(false);
    expect(useOtpFlowStore.getState().step).toBe('OTP_VERIFY');
  });

  it('restores the OTP step and remaining cooldown after refresh', () => {
    useOtpFlowStore.getState().setPhoneInput('09121234567');
    useOtpFlowStore.getState().moveToOtpVerify({
      requestId: 'persisted-request',
      otpExpiresAtEpochMs: Date.now() + 90_000,
    });

    renderLogin();

    expect(screen.getByText('کد تایید ۵ رقمی')).toBeInTheDocument();
    const resendButton = screen.getByRole('button', { name: /ارسال مجدد \(01:/ });
    expect(resendButton).toBeDisabled();
  });
});

describe('Session security behavior', () => {
  beforeEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    useSessionStore.getState().clearSession();
    useOtpFlowStore.getState().clearFlow();
  });

  afterEach(() => {
    cleanup();
  });

  it('clears persisted session data on logout', () => {
    useSessionStore.getState().setSession('jwt-token', {
      id: 1,
      username: 'normal-user',
      email: 'normal-user@archive.local',
      firstName: 'Normal',
      lastName: 'User',
      roles: ['Viewer'],
      permissions: [],
    });

    expect(window.localStorage.getItem('archive-session')).toContain('jwt-token');

    useSessionStore.getState().clearSession();

    expect(window.localStorage.getItem('archive-session')).toBeNull();
    expect(useSessionStore.getState().isAuthenticated).toBe(false);
  });

  it('clears user-scoped query data on logout', () => {
    queryClient.setQueryData(['classifications'], [
      { id: 1, title: 'Admin-only classification' },
    ]);

    expect(queryClient.getQueryData(['classifications'])).toBeDefined();

    useSessionStore.getState().clearSession();

    expect(queryClient.getQueryData(['classifications'])).toBeUndefined();
  });
});
