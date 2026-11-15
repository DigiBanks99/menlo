import { expect, test } from '@playwright/test';
import { mockAuthUser } from './fixtures/mocks';

test.describe('Signed-in landing', () => {
  test('a signed-in member lands on home, not on an onboarding step', async ({ page }) => {
    await mockAuthUser(page);
    await page.route(/\/api\/budgets$/, (route) => route.fulfill({ json: [] }));

    await page.goto('/');

    await expect(page).toHaveURL(/\/$/);
    await expect(page).not.toHaveURL(/onboarding/);
  });
});
