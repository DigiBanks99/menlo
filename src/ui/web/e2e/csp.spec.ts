import { expect, Page, test } from '@playwright/test';

// The Content-Security-Policy header is set by the API, not the Angular dev server, so this
// spec only means something against the API origin (ideally the bundled production image):
//   Menlo__BundledUi=true aspire start   # then
//   E2E_BASE_URL=http://localhost:8080 pnpm exec playwright test e2e/csp.spec.ts
const API_ORIGIN = process.env['E2E_BASE_URL'];

const BUDGET_ID = 'budget-e2e-csp-001';
const PARENT_CAT_ID = 'cat-e2e-csp-food';
const LEAF_CAT_ID = 'cat-e2e-csp-groceries';

const mockUser = {
  id: 'user-e2e-001',
  email: 'test@menlo.local',
  displayName: 'E2E Test User',
  roles: ['Menlo.User'],
  onboarding: { isComplete: true, pendingTasks: [] },
};

const mockBudget = {
  id: BUDGET_ID,
  year: 2025,
  householdId: 'hh-e2e-001',
  status: 'Active',
  categories: [
    { id: PARENT_CAT_ID, name: 'Food', parentId: null },
    { id: LEAF_CAT_ID, name: 'Groceries', parentId: PARENT_CAT_ID },
  ],
  totalPlannedMonthlyAmount: { amount: 0, currency: 'ZAR' },
};

const mockCategoryTree = [
  {
    id: PARENT_CAT_ID,
    name: 'Food',
    description: '',
    budgetFlow: 'Expense',
    isDeleted: false,
    children: [
      {
        id: LEAF_CAT_ID,
        name: 'Groceries',
        description: '',
        budgetFlow: 'Expense',
        isDeleted: false,
        children: [],
      },
    ],
  },
];

const mockSummary = {
  budgetId: BUDGET_ID,
  year: 2025,
  month: null,
  income: [],
  expenses: [],
  netPlanned: 0,
  netRealized: null,
  netSpent: null,
};

async function setupMocks(page: Page): Promise<void> {
  await page.route('**/auth/user', (route) => route.fulfill({ json: mockUser }));
  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}$`), (route) =>
    route.fulfill({ json: mockBudget }),
  );
  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}/categories(\\?.*)?$`), (route) =>
    route.fulfill({ json: mockCategoryTree }),
  );
  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}/summary`), (route) =>
    route.fulfill({ json: mockSummary }),
  );
  await page.route(
    new RegExp(`/api/budgets/${BUDGET_ID}/categories/${LEAF_CAT_ID}/items$`),
    (route) => route.fulfill({ json: [] }),
  );
}

/** Records every CSP violation the page reports, from the DOM event and the console. */
async function trackCspViolations(page: Page): Promise<string[]> {
  const violations: string[] = [];
  await page.exposeFunction('reportCspViolation', (detail: string) => violations.push(detail));
  await page.addInitScript(() => {
    document.addEventListener('securitypolicyviolation', (event) => {
      (window as unknown as { reportCspViolation: (d: string) => void }).reportCspViolation(
        `${event.violatedDirective} blocked ${event.blockedURI.slice(0, 80)}`,
      );
    });
  });
  page.on('console', (message) => {
    if (message.text().includes('Content Security Policy')) {
      violations.push(message.text().slice(0, 200));
    }
  });
  return violations;
}

test.describe('Content Security Policy', () => {
  test.skip(!API_ORIGIN, 'Set E2E_BASE_URL to the API origin to verify the CSP');

  test.use({ baseURL: API_ORIGIN });

  test('serves the SPA with a CSP that allows data: images and no inline scripts', async ({
    page,
  }) => {
    const response = await page.goto('/');
    const csp = response?.headers()['content-security-policy'] ?? '';

    expect(csp).toContain("img-src 'self' data:");
    expect(csp).toMatch(/script-src [^;]*'self'/);
    if (process.env['E2E_EXPECT_PRODUCTION_CSP']) {
      expect(csp).not.toMatch(/script-src [^;]*'unsafe-inline'/);
    }
  });

  test('renders the budget item form dropdown arrows without CSP violations', async ({
    page,
  }) => {
    const violations = await trackCspViolations(page);
    await setupMocks(page);

    await page.goto(`/budgets/${BUDGET_ID}`);
    await expect(page.getByTestId('budget-year')).toHaveText('2025 Budget');
    await page.getByTestId(`btn-view-items-${LEAF_CAT_ID}`).click();
    await expect(page.getByTestId('budget-items-workspace')).toBeVisible();

    await page.getByTestId('btn-open-single-create').click();
    const select = page.getByTestId('select-budgetFlow');
    await expect(select).toBeVisible();

    const backgroundImage = await select.evaluate((el) => getComputedStyle(el).backgroundImage);
    expect(backgroundImage).toContain('data:image/svg+xml');
    expect(violations).toEqual([]);
  });
});
