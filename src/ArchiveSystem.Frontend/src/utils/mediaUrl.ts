const configuredApiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api';
const configuredApiProxyTarget = import.meta.env.VITE_API_PROXY_TARGET ?? '';

const trimTrailingSlash = (value: string): string => value.replace(/\/+$/, '');

const getMediaHost = (): string => {
  if (configuredApiBaseUrl.startsWith('http://') || configuredApiBaseUrl.startsWith('https://')) {
    return trimTrailingSlash(new URL(configuredApiBaseUrl).origin);
  }

  if (configuredApiProxyTarget.startsWith('http://') || configuredApiProxyTarget.startsWith('https://')) {
    return trimTrailingSlash(configuredApiProxyTarget);
  }

  if (typeof window !== 'undefined') {
    return trimTrailingSlash(window.location.origin);
  }

  return '';
};

export const resolveMediaUrl = (path?: string | null): string | null => {
  if (!path) {
    return null;
  }

  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('blob:')) {
    return path;
  }

  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  const host = getMediaHost();
  return host ? `${host}${normalizedPath}` : normalizedPath;
};
