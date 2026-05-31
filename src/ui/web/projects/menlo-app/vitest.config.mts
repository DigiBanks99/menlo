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
    coverage: {
      reportsDirectory: '../../coverage/projects/menlo-app',
      provider: 'v8',
      reporter: [
        ['cobertura', { file: 'coverage.cobertura.xml' }],
        ['clover', { file: 'clover.xml' }],
        'json',
        'html',
      ],
      all: true,
      include: ['src/**/*.ts'],
      exclude: [
        'src/app/core/auth/auth.interceptor.ts',
        'src/main.ts',
        'src/app/app.config.ts',
        'src/environments/**',
        'src/stories/**',
        'src/test-setup.ts',
      ],
      thresholds: {
        lines: 85,
        functions: 85,
        branches: 85,
        statements: 85,
      },
    },
  },
  resolve: {
    alias: {
      'menlo-lib': path.resolve(__dirname, '../../dist/menlo-lib'),
      'data-access-menlo-api': path.resolve(__dirname, '../../dist/data-access-menlo-api'),
      'shared-util': path.resolve(__dirname, '../../dist/shared-util'),
    },
  },
}));
