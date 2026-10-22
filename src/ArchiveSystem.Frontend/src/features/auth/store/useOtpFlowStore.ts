import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

export type OtpFlowStep = 'PHONE_INPUT' | 'OTP_VERIFY' | 'RESET_PASS';

interface OtpFlowState {
  step: OtpFlowStep;
  phoneNumber: string;
  requestId: string | null;
  otpExpiresAtEpochMs: number | null;
  resetToken: string | null;
  setPhoneInput: (phoneNumber: string) => void;
  moveToOtpVerify: (payload: { requestId: string; otpExpiresAtEpochMs: number }) => void;
  moveToResetPassword: (resetToken: string) => void;
  restartFlow: () => void;
  clearFlow: () => void;
}

const initialState: Pick<OtpFlowState, 'step' | 'phoneNumber' | 'requestId' | 'otpExpiresAtEpochMs' | 'resetToken'> = {
  step: 'PHONE_INPUT',
  phoneNumber: '',
  requestId: null,
  otpExpiresAtEpochMs: null,
  resetToken: null,
};

const storage = createJSONStorage(() => sessionStorage);

export const useOtpFlowStore = create<OtpFlowState>()(
  persist(
    (set) => ({
      ...initialState,
      setPhoneInput: (phoneNumber) =>
        set({
          phoneNumber,
          step: 'PHONE_INPUT',
        }),
      moveToOtpVerify: ({ requestId, otpExpiresAtEpochMs }) =>
        set((state) => ({
          ...state,
          step: 'OTP_VERIFY',
          requestId,
          otpExpiresAtEpochMs,
        })),
      moveToResetPassword: (resetToken) =>
        set((state) => ({
          ...state,
          step: 'RESET_PASS',
          resetToken,
        })),
      restartFlow: () =>
        set((state) => ({
          ...state,
          step: 'PHONE_INPUT',
          requestId: null,
          otpExpiresAtEpochMs: null,
          resetToken: null,
        })),
      clearFlow: () => set(initialState),
    }),
    {
      name: 'archive:otp-flow',
      storage,
      partialize: (state) => ({
        step: state.step,
        phoneNumber: state.phoneNumber,
        requestId: state.requestId,
        otpExpiresAtEpochMs: state.otpExpiresAtEpochMs,
        resetToken: state.resetToken,
      }),
    },
  ),
);
