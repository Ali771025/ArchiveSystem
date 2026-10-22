import { api } from '@/services/api';

// Authentication requests use the same persisted session as every other API request.
export const createAuthHttpClient = () => api;

export const authHttpClient = api;
