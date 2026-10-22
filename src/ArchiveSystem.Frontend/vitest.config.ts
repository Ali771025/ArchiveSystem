import { defineConfig, mergeConfig } from 'vitest/config';
import viteConfig from './vite.config';

export default defineConfig((environment) => mergeConfig(viteConfig(environment), {
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    maxWorkers: 2,
    testTimeout: 15000,
  },
}));
