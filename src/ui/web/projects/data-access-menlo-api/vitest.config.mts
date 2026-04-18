import path from 'path';
import { defineConfig } from 'vite';

export default defineConfig(({ mode }) => ({
  resolve: {
    alias: {
      'shared-util': path.resolve(__dirname, '../../dist/shared-util'),
    },
  },
  test: {
    globals: true,
    setupFiles: [path.resolve(__dirname, 'src/test-setup.ts')],
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
