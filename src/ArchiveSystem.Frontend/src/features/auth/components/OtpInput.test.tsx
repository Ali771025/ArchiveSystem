/* @vitest-environment jsdom */
import '@testing-library/jest-dom/vitest';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { useState } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OtpInput } from '@/features/auth/components/OtpInput';
import { VerifyOtpForm } from '@/features/auth/components/VerifyOtpForm';

const requestOtpMutateAsync = vi.fn();
const verifyOtpMutateAsync = vi.fn();

vi.mock('@/features/auth/hooks/useOtpAuthMutations', async () => {
  return {
    mapAuthErrorMessage: () => 'خطا',
    useRequestOtpMutation: () => ({
      isPending: false,
      mutateAsync: requestOtpMutateAsync,
    }),
    useVerifyOtpAndLoginMutation: () => ({
      isPending: false,
      mutateAsync: verifyOtpMutateAsync,
    }),
  };
});

const OtpHarness = () => {
  const [value, setValue] = useState('');

  return (
    <div>
      <OtpInput length={5} value={value} onChange={setValue} autoFocus />
      <div data-testid="otp-value">{value}</div>
    </div>
  );
};

describe('OtpInput', () => {
  it('distributes pasted value across input boxes', () => {
    render(<OtpHarness />);

    const firstInput = screen.getByLabelText('otp-digit-1');
    const container = firstInput.parentElement as HTMLElement;

    fireEvent.paste(container, {
      clipboardData: {
        getData: () => '12345',
      },
    });

    expect(screen.getByLabelText('otp-digit-1')).toHaveValue('1');
    expect(screen.getByLabelText('otp-digit-2')).toHaveValue('2');
    expect(screen.getByLabelText('otp-digit-3')).toHaveValue('3');
    expect(screen.getByLabelText('otp-digit-4')).toHaveValue('4');
    expect(screen.getByLabelText('otp-digit-5')).toHaveValue('5');
    expect(screen.queryByLabelText('otp-digit-6')).not.toBeInTheDocument();
    expect(screen.getByTestId('otp-value')).toHaveTextContent('12345');
  });

  it('supports backspace behavior across boxes', () => {
    render(<OtpHarness />);

    const firstInput = screen.getByLabelText('otp-digit-1');
    const secondInput = screen.getByLabelText('otp-digit-2');

    fireEvent.change(firstInput, { target: { value: '1' } });
    fireEvent.change(secondInput, { target: { value: '2' } });

    fireEvent.keyDown(secondInput, { key: 'Backspace' });
    expect(secondInput).toHaveValue('');

    fireEvent.keyDown(secondInput, { key: 'Backspace' });
    expect(firstInput).toHaveValue('');
  });
});

describe('VerifyOtpForm', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    requestOtpMutateAsync.mockReset();
    verifyOtpMutateAsync.mockReset();
    requestOtpMutateAsync.mockResolvedValue({ succeeded: true, message: 'ارسال شد' });
    verifyOtpMutateAsync.mockResolvedValue({ succeeded: false, message: 'نامعتبر' });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('keeps resend disabled until countdown finishes', () => {
    render(
      <VerifyOtpForm
        mobileNumber="09123456789"
        initialExpiresAtEpochMs={Date.now() + 3000}
        onBack={vi.fn()}
        onVerified={vi.fn()}
      />,
    );

    const resendButton = screen.getByRole('button', { name: /ارسال مجدد/i });
    expect(resendButton).toBeDisabled();

    act(() => {
      vi.advanceTimersByTime(3500);
    });

    expect(screen.getByRole('button', { name: 'ارسال مجدد کد' })).toBeEnabled();
  });
});
