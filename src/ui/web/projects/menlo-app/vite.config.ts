/// <reference types="vitest" />
import angular from '@analogjs/vite-plugin-angular';
import { nxViteTsPaths } from '@nx/vite/plugins/nx-tsconfig-paths.plugin';
import { defineConfig } from 'vite';

export default defineConfig(({ mode }) => ({
  root: __dirname,
  cacheDir: '../../node_modules/.vite/projects/menlo-app',
  plugins: [angular(), nxViteTsPaths()],
  test: {
    name: 'menlo-app',
    globals: true,
    setupFiles: ['src/test-setup.ts'],
    environment: 'jsdom',
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    reporters: ['default'],
    coverage: {
      reportsDirectory: '../../coverage/projects/menlo-app',
      provider: 'v8',
    },
    server: {
      deps: {
        inline: ['@angular/core', '@angular/platform-browser'],
      },
    },
  },
  define: {
    'import.meta.vitest': mode !== 'production',
  },
}));
