import { create } from 'zustand';
import { queryClient } from '@/services/queryClient';
import { resolveRolePermissions } from '@/services/rolePermissions';
import type { PermissionKey, SessionUser } from '@/types/archive';

const STORAGE_KEY = 'archive-session';

interface SessionState {
  token: string | null;
  user: SessionUser | null;
  isAuthenticated: boolean;
  setSession: (token: string, user: SessionUser) => void;
  clearSession: () => void;
  hasPermission: (permission: PermissionKey) => boolean;
}

const loadSession = (): Pick<SessionState, 'token' | 'user' | 'isAuthenticated'> => {
  if (typeof window === 'undefined') {
    return { token: null, user: null, isAuthenticated: false };
  }

  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return { token: null, user: null, isAuthenticated: false };
  }

  try {
    const parsed = JSON.parse(raw) as { token: string; user: SessionUser };
    const user = {
      ...parsed.user,
      permissions: parsed.user.permissions ?? resolveRolePermissions(parsed.user.roles ?? []),
    };
    return { token: parsed.token, user, isAuthenticated: true };
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return { token: null, user: null, isAuthenticated: false };
  }
};

export const useSessionStore = create<SessionState>((set, get) => ({
  ...loadSession(),
  setSession: (token, user) => {
    const normalizedUser = {
      ...user,
      permissions: user.permissions ?? resolveRolePermissions(user.roles),
    };

    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({ token, user: normalizedUser }));
    }

    set({ token, user: normalizedUser, isAuthenticated: true });
  },
  clearSession: () => {
    if (typeof window !== 'undefined') {
      localStorage.removeItem(STORAGE_KEY);
    }

    set({ token: null, user: null, isAuthenticated: false });
    queryClient.clear();
  },
  hasPermission: (permission) => {
    const state = get();
    if (!state.user) {
      return false;
    }

    if (state.user.roles.some((role) => ['admin', 'superuser'].includes(role.toLowerCase()))) {
      return true;
    }

    return state.user.permissions.includes(permission);
  },
}));

