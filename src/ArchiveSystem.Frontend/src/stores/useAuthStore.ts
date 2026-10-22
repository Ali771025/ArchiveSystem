// src/stores/useAuthStore.ts
import { create } from 'zustand';

interface AuthState {
  user: any | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  login: (username: string, password: string) => Promise<boolean>;
  logout: () => void;
  setToken: (token: string) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  token: typeof window !== 'undefined' ? localStorage.getItem('token') : null,
  isAuthenticated: false,
  loading: false,

  login: async (username: string, password: string) => {
    set({ loading: true });
    
    // Mock authentication
    if (username && password) {
      const mockUser = {
        id: '1',
        username,
        email: `${username}@example.com`,
        role: 'admin',
        permissions: ['read', 'write', 'delete', 'manage']
      };
      
      const token = `mock-jwt-token-${Date.now()}`;
      localStorage.setItem('token', token);
      
      set({
        user: mockUser,
        token,
        isAuthenticated: true,
        loading: false
      });
      
      return true;
    }
    
    set({ loading: false });
    return false;
  },

  logout: () => {
    localStorage.removeItem('token');
    set({
      user: null,
      token: null,
      isAuthenticated: false
    });
  },

  setToken: (token: string) => {
    localStorage.setItem('token', token);
    set({ token, isAuthenticated: !!token });
  }
}));
