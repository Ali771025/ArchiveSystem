import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'node:path';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiProxyTarget = env.VITE_API_PROXY_TARGET ?? 'http://localhost:5194';

  return {
    plugins: [react()],
    define: {
      "import.meta.env.VITE_ENABLE_OFFLINE_FALLBACK": JSON.stringify(
        mode === "showcase" ? "true" : (env.VITE_ENABLE_OFFLINE_FALLBACK ?? "false"),
      ),
    },
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      proxy: {
        '/api': {
          target: apiProxyTarget,
          changeOrigin: true,
          secure: false,
        },
        '/uploads': {
          target: apiProxyTarget,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  };
});
