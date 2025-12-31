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
      reportsDirectory: '../../coverage/projects/data-access-menlo-api',
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
