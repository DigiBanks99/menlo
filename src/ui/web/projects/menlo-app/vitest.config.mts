import path from 'path';
import { defineConfig } from 'vite';

import angular from '@analogjs/vite-plugin-angular';

export default defineConfig(({ mode }) => ({
  plugins: [angular()],
  test: {
    globals: true,
    setupFiles: ['src/test-setup.ts'],
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    reporters: ['default'],
  },
  resolve: {
    alias: {
      'menlo-lib': path.resolve(__dirname, '../../dist/menlo-lib'),
      'data-access-menlo-api': path.resolve(__dirname, '../../dist/data-access-menlo-api'),
      'shared-util': path.resolve(__dirname, '../../dist/projects/shared-util'),
    },
  },
}));
