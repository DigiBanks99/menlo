/// <reference types='vitest' />
import angular from '@analogjs/vite-plugin-angular';
import { nxCopyAssetsPlugin } from '@nx/vite/plugins/nx-copy-assets.plugin';
import { nxViteTsPaths } from '@nx/vite/plugins/nx-tsconfig-paths.plugin';
import { defineConfig } from 'vite';
import dts from 'vite-plugin-dts';

export default defineConfig(({ mode }) => ({
  root: __dirname,
  cacheDir: '../../node_modules/.vite/projects/data-access-menlo-api',
  plugins: [
    angular(),
    nxViteTsPaths(),
    nxCopyAssetsPlugin(['*.md']),
    dts({
      pathsToAliases: false,
    }),
  ],
  test: {
    name: 'data-access-menlo-api',
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.{test,spec}.{js,mjs,cjs,ts,mts,cts,jsx,tsx}'],
    reporters: ['default'],
    coverage: {
      reportsDirectory: '../../coverage/projects/data-access-menlo-api',
      provider: 'v8' as const,
    },
    server: {
      deps: {
        inline: [
          '@angular/core',
          '@angular/core/testing',
          '@angular/platform-browser',
          '@angular/platform-browser/testing',
          '@angular/common',
          '@angular/router',
          '@angular/forms',
          '@angular/platform-browser-dynamic',
          '@angular/compiler',
          '@angular/animations',
          //@angular\//,
        ],
      },
    },
  },
  // Configuration for building your library.
  // See: https://vitejs.dev/guide/build.html#library-mode
  build: {
    outDir: '../../dist/projects/data-access-menlo-api',
    emptyOutDir: true,
    lib: {
      entry: 'src/index.ts',
      name: 'data-access-menlo-api',
      fileName: 'index',
      formats: ['es' as const],
    },
    rollupOptions: {
      external: ['@angular/core', '@angular/common', '@angular/platform-browser'],
    },
  },
  define: {
    'import.meta.vitest': mode !== 'production',
  },
}));
