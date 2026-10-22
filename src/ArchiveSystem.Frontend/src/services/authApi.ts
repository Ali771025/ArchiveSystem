import { api } from '@/services/api';
import { canUseOfflineFallback } from '@/services/errorMapper';
import { resolveRolePermissions } from '@/services/rolePermissions';
import type { SessionUser } from '@/types/archive';

interface LoginRequest {
  username: string;
  password: string;
}

interface RegisterRequest {
  username: string;
  password: string;
  confirmPassword: string;
}

interface ForgotPasswordRequest {
  usernameOrEmail: string;
}

interface AuthenticatedLoginResponse {
  requiresTwoFactor?: false;
  token: string;
  user: {
    id: number;
    username: string;
    email: string;
    firstName: string;
    lastName: string;
    roles: string[];
    permissions?: SessionUser['permissions'];
  };
}

interface TwoFactorChallengeResponse {
  requiresTwoFactor: true;
  mobileNumber: string;
  otpExpiresAtUtc?: string;
}

export type PasswordLoginResult =
  | { requiresTwoFactor: false; token: string; user: SessionUser }
  | { requiresTwoFactor: true; mobileNumber: string; otpExpiresAtUtc?: string };

const toSessionUser = (user: AuthenticatedLoginResponse['user']): SessionUser => {
  return {
    id: user.id,
    username: user.username,
    email: user.email,
    firstName: user.firstName,
    lastName: user.lastName,
    roles: user.roles,
    permissions: user.permissions ?? resolveRolePermissions(user.roles),
  };
};

const mockLogin = async ({ username }: LoginRequest): Promise<PasswordLoginResult> => {
  const role = username === 'admin' ? 'Admin' : username === 'viewer' ? 'Viewer' : 'ArchiveOperator';
  const user: SessionUser = toSessionUser({
    id: role === 'Admin' ? 1 : role === 'Viewer' ? 3 : 2,
    username,
    email: `${username}@archive.local`,
    firstName: role === 'Admin' ? 'مدیر' : 'کاربر',
    lastName: role === 'Viewer' ? 'مشاهده' : 'آرشیو',
    roles: [role],
  });

  await new Promise((resolve) => setTimeout(resolve, 250));
  return { requiresTwoFactor: false, token: `offline-token-${Date.now()}`, user };
};

const mockRegister = async ({ username }: RegisterRequest): Promise<{ token: string; user: SessionUser }> => {
  await new Promise((resolve) => setTimeout(resolve, 300));

  return {
    token: `offline-register-token-${Date.now()}`,
    user: toSessionUser({
      id: Math.floor(Math.random() * 100000) + 100,
      username,
      email: `${username}@archive.local`,
      firstName: 'کاربر',
      lastName: 'جدید',
      roles: ['Viewer'],
    }),
  };
};

export const authApi = {
  async login(request: LoginRequest): Promise<PasswordLoginResult> {
    try {
      const response = await api.post<AuthenticatedLoginResponse | TwoFactorChallengeResponse>('/auth/login', request);
      if (response.data.requiresTwoFactor) {
        return {
          requiresTwoFactor: true,
          mobileNumber: response.data.mobileNumber,
          otpExpiresAtUtc: response.data.otpExpiresAtUtc,
        };
      }

      return {
        requiresTwoFactor: false,
        token: response.data.token,
        user: toSessionUser(response.data.user),
      };
    } catch (error) {
      if (!canUseOfflineFallback(error)) {
        throw error;
      }

      return mockLogin(request);
    }
  },

  async register(request: RegisterRequest): Promise<{ token: string; user: SessionUser }> {
    const username = request.username.trim();
    if (!username || !request.password || !request.confirmPassword) {
      throw new Error('همه فیلدها را وارد کنید.');
    }

    if (request.password !== request.confirmPassword) {
      throw new Error('تکرار گذرواژه با گذرواژه یکسان نیست.');
    }

    // Current backend register endpoint expects additional profile fields.
    const payload = {
      username,
      password: request.password,
      confirmPassword: request.confirmPassword,
      email: `${username}@archive.local`,
      firstName: username,
      lastName: 'User',
    };

    try {
      const response = await api.post<AuthenticatedLoginResponse>('/auth/register', payload);
      return {
        token: response.data.token,
        user: toSessionUser(response.data.user),
      };
    } catch (error) {
      if (!canUseOfflineFallback(error)) {
        throw error;
      }

      return mockRegister(request);
    }
  },

  async forgotPassword(request: ForgotPasswordRequest): Promise<{ success: boolean; message: string }> {
    const usernameOrEmail = request.usernameOrEmail.trim();
    if (!usernameOrEmail) {
      throw new Error('نام کاربری یا ایمیل را وارد کنید.');
    }

    await new Promise((resolve) => setTimeout(resolve, 250));
    return {
      success: true,
      message: 'در این نسخه API مسیر بازیابی گذرواژه فعال نیست. لطفا با مدیر سامانه تماس بگیرید.',
    };
  },
};

export type { ForgotPasswordRequest, LoginRequest, RegisterRequest };
