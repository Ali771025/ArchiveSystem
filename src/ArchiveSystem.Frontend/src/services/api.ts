import axios, { AxiosError } from 'axios';
import { mapApiError } from '@/services/errorMapper';
import { useSessionStore } from '@/stores/useSessionStore';

const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api';
const baseURL = configuredApiBaseUrl.endsWith('/')
  ? configuredApiBaseUrl.slice(0, -1)
  : configuredApiBaseUrl;

export const api = axios.create({
  baseURL,
  timeout: 30000,
  ...(import.meta.env.MODE === 'showcase' ? {
    adapter: async (config) => {
      throw new AxiosError('Portfolio demo: offline synthetic data', 'ERR_NETWORK', config);
    },
  } : {}),
});

api.interceptors.request.use((config) => {
  const token = useSessionStore.getState().token;
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    const mapped = mapApiError(error);
    if (mapped.status === 401) {
      useSessionStore.getState().clearSession();
      if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
        window.location.assign('/login');
      }
    }

    return Promise.reject(error);
  },
);

