import { expect, Page, test } from '@playwright/test';

const EXISTING_HOUSEHOLD_ID = 'household-existing-001';
const CREATED_HOUSEHOLD_ID = 'household-created-001';
const CURRENT_YEAR = new Date().getFullYear();

const homeBudget = {
  id: 'budget-home-001',
  year: CURRENT_YEAR,
  householdId: EXISTING_HOUSEHOLD_ID,
  status: 'Active',
  categories: [],
  totalPlannedMonthlyAmount: { amount: 12500, currency: 'ZAR' },
};

type OnboardingHarness = {
  completeOnboarding(): void;
};

async function setupOnboardingMocks(page: Page): Promise<OnboardingHarness> {
  let onboardingComplete = false;
  const households = [{ id: EXISTING_HOUSEHOLD_ID, name: 'Existing Family' }];

  await page.route('**/auth/user', async (route) => {
    await route.fulfill({
      json: {
        id: 'user-e2e-001',
        email: 'test@menlo.local',
        displayName: 'Onboarding Test User',
        roles: ['Menlo.User'],
        onboarding: onboardingComplete
          ? { isComplete: true, pendingTasks: [] }
          : { isComplete: false, pendingTasks: ['SelectHousehold'] },
      },
    });
  });

  await page.route('**/api/households', async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ json: { households } });
      return;
    }

    if (route.request().method() === 'POST') {
      const request = route.request().postDataJSON() as { name: string };
      onboardingComplete = true;
      households.push({ id: CREATED_HOUSEHOLD_ID, name: request.name });
      await route.fulfill({ status: 201, json: { id: CREATED_HOUSEHOLD_ID, name: request.name } });
      return;
    }

    await route.continue();
  });

  await page.route(`**/api/households/${EXISTING_HOUSEHOLD_ID}/join`, async (route) => {
    onboardingComplete = true;
    await route.fulfill({ status: 204, body: '' });
  });

  await page.route(new RegExp(`/api/budgets/${CURRENT_YEAR}$`), async (route) => {
    await route.fulfill({ status: 200, json: homeBudget });
  });

  return {
    completeOnboarding() {
      onboardingComplete = true;
    },
  };
}

test.describe('Onboarding flow', () => {
  test('redirects incomplete users to onboarding and renders both onboarding actions', async ({ page }) => {
    await setupOnboardingMocks(page);

    await page.goto('/');

    await page.waitForURL(/\/onboarding(\?.*)?$/);
    await expect(page.getByTestId('onboarding-title')).toHaveText('Welcome to Menlo');
    await expect(page.getByTestId('join-household-section')).toContainText('Join Existing Household');
    await expect(page.getByTestId('create-household-section')).toContainText('Create New Household');
    await expect(page.getByTestId('household-list')).toContainText('Existing Family');
  });

  test('allows a user to create a household and continue to the requested route', async ({ page }) => {
    await setupOnboardingMocks(page);

    await page.goto('/analytics');
    await page.waitForURL(/\/onboarding\?returnUrl=%2Fanalytics$/);

    await page.getByTestId('household-name-input').fill('Test Family');
    await page.getByTestId('create-household-button').click();

    await page.waitForURL('/analytics');
    await expect(page).toHaveURL(/\/analytics$/);
  });

  test('allows a user to join an existing household and land on home', async ({ page }) => {
    await setupOnboardingMocks(page);

    await page.goto('/');
    await page.waitForURL(/\/onboarding(\?.*)?$/);

    await page.getByTestId(`join-household-${EXISTING_HOUSEHOLD_ID}`).click();

    await page.waitForURL('/');
    await expect(page.getByRole('heading', { name: 'Menlo Home Management' })).toBeVisible();
  });

  test('prevents re-entry to onboarding after completion', async ({ page }) => {
    const harness = await setupOnboardingMocks(page);
    harness.completeOnboarding();

    await page.goto('/onboarding');

    await page.waitForURL('/');
    await expect(page.getByRole('heading', { name: 'Menlo Home Management' })).toBeVisible();
  });

  test('persists onboarding completion across page refresh', async ({ page }) => {
    await setupOnboardingMocks(page);

    await page.goto('/');
    await page.waitForURL(/\/onboarding(\?.*)?$/);
    await page.getByTestId('household-name-input').fill('Refresh Family');
    await page.getByTestId('create-household-button').click();

    await page.waitForURL('/');
    await page.reload();

    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('heading', { name: 'Menlo Home Management' })).toBeVisible();
  });
});
