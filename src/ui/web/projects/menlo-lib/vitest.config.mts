import path from 'path';
import { defineConfig } from 'vite';

import angular from '@analogjs/vite-plugin-angular';

export default defineConfig(({ mode }) => ({
  root: __dirname,
  plugins: [
    angular({
      tsconfig: path.resolve(__dirname, './tsconfig.spec.json'),
      include: ['/src/**/*.{spec,test}.{ts,mts,cts,tsx,jsx}'],
      workspaceRoot: __dirname,
    }),
  ],
  resolve: {
    alias: {
      'shared-util': path.resolve(__dirname, '../../dist/shared-util'),
    },
  },
  test: {
    globals: true,
    setupFiles: ['src/test-setup.ts'],
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    reporters: ['default'],
    coverage: {
      reportsDirectory: '../../coverage/projects/menlo-lib',
      provider: 'v8' as const,
      reporter: [
        ['cobertura', { file: 'coverage.cobertura.xml' }],
        ['clover', { file: 'clover.xml' }],
        'json',
        'html',
      ],
      all: true,
      include: ['src/**/*.ts'],
      exclude: ['src/**/*.stories.ts', 'src/test-setup.ts', 'src/lib/foundations/**/*.ts'],
      thresholds: {
        lines: 100,
        functions: 100,
        branches: 100,
        statements: 100,
      },
    },
  },
}));
