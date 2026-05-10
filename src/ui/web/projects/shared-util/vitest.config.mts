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
  test: {
    globals: true,
    setupFiles: ['src/test-setup.ts'],
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    reporters: ['default'],
    coverage: {
      reportsDirectory: '../../coverage/projects/shared-util',
      provider: 'v8' as const,
      reporter: [
        ['cobertura', { file: 'coverage.cobertura.xml' }],
        ['clover', { file: 'clover.xml' }],
        'json',
        'html',
      ],
    },
  },
}));
