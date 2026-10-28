import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  outputDir: './test-results/playwright',
  reporter: [
    ['html', { outputFolder: './test-results/playwright-report', open: 'never' }],
    ['junit', { outputFile: './test-results/playwright/results.xml' }],
  ],
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  // Targeting an already running stack (e.g. the bundled API image) needs no dev server.
  webServer: process.env['E2E_BASE_URL'] ? undefined : {
    command: 'pnpm run start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env['CI'],
    timeout: 120_000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
