import createCache from '@emotion/cache';
import { CacheProvider } from '@emotion/react';
import rtlPlugin from '@mui/stylis-plugin-rtl';
import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactNode, useEffect, useMemo } from 'react';
import { queryClient } from '@/services/queryClient';
import { createAppTheme } from '@/theme';
import { AppSnackbar } from '@/components/common/AppSnackbar';
import { ConfirmProvider } from '@/components/common/ConfirmContext';
import { useUIStore } from '@/stores/useUIStore';

interface AppProvidersProps {
  children: ReactNode;
}

const rtlCache = createCache({
  key: 'mui-rtl',
  stylisPlugins: [rtlPlugin],
});

export const AppProviders = ({ children }: AppProvidersProps) => {
  const darkMode = useUIStore((state) => state.darkMode);
  const appTheme = useMemo(() => createAppTheme(darkMode ? 'dark' : 'light'), [darkMode]);

  useEffect(() => {
    document.documentElement.setAttribute('dir', 'rtl');
    document.documentElement.lang = 'fa';
    document.documentElement.dataset.theme = darkMode ? 'dark' : 'light';
    document.documentElement.style.colorScheme = darkMode ? 'dark' : 'light';
    document.body.setAttribute('dir', 'rtl');
  }, [darkMode]);

  return (
    <QueryClientProvider client={queryClient}>
      <CacheProvider value={rtlCache}>
        <ThemeProvider theme={appTheme}>
          <CssBaseline />
          <ConfirmProvider>{children}</ConfirmProvider>
          <AppSnackbar />
        </ThemeProvider>
      </CacheProvider>
    </QueryClientProvider>
  );
};

