import { expect, Page, test } from '@playwright/test';
import { budgetDetail, emptySummary, mockAuthUser } from './fixtures/mocks';

// ─── Test fixture data ───────────────────────────────────────────────────────

const CURRENT_YEAR = new Date().getFullYear();
const CURRENT_BUDGET_ID = 'budget-e2e-list-current';
const CREATED_BUDGET_ID = 'budget-e2e-list-created';

const mockCurrentBudgetListItem = {
  id: CURRENT_BUDGET_ID,
  year: CURRENT_YEAR,
  status: 'Active',
  plannedExpenses: { amount: 10000, currency: 'ZAR' },
  spentExpenses: { amount: 8500, currency: 'ZAR' },
  categoryCount: 4,
};

// ─── Mock setup helper ────────────────────────────────────────────────────────

/**
 * Mocks the BFF auth, the budget list, budget creation and the detail page's reads.
 * Returns the years POSTed to /api/budgets/{year}.
 */
async function setupMocks(page: Page, budgets: unknown[]): Promise<number[]> {
  const createdYears: number[] = [];

  await mockAuthUser(page);

  // Budget list
  await page.route(/\/api\/budgets$/, (route) => route.fulfill({ json: budgets }));

  // Budget create (POST /api/budgets/{year})
  await page.route(/\/api\/budgets\/\d{4}$/, (route) => {
    const year = Number(new URL(route.request().url()).pathname.split('/').pop());
    createdYears.push(year);
    return route.fulfill({ status: 201, json: budgetDetail(CREATED_BUDGET_ID, year, 'Draft') });
  });

  // Budget detail reads for whichever budget the list navigates to
  for (const [id, year, status] of [
    [CURRENT_BUDGET_ID, CURRENT_YEAR, 'Active'],
    [CREATED_BUDGET_ID, CURRENT_YEAR + 1, 'Draft'],
  ] as const) {
    await page.route(new RegExp(`/api/budgets/${id}$`), (route) =>
      route.fulfill({ json: budgetDetail(id, year, status) }),
    );
    await page.route(new RegExp(`/api/budgets/${id}/categories(\\?.*)?$`), (route) =>
      route.fulfill({ json: [] }),
    );
    await page.route(new RegExp(`/api/budgets/${id}/summary`), (route) =>
      route.fulfill({ json: emptySummary(id, year) }),
    );
  }

  return createdYears;
}

// ─── Tests ────────────────────────────────────────────────────────────────────

test('lists real budgets with this month’s spending and opens the detail page', async ({
  page,
}) => {
  await setupMocks(page, [mockCurrentBudgetListItem]);

  await page.goto('/budgets');

  const card = page.getByTestId(`budget-card-${CURRENT_BUDGET_ID}`);
  await expect(card).toContainText(`Budget ${CURRENT_YEAR}`);
  await expect(card).toContainText('4 categories');
  await expect(page.getByTestId(`budget-spend-${CURRENT_BUDGET_ID}`)).toContainText('85%');
  await expect(page.getByText('Monthly Household Budget')).toHaveCount(0);

  await page.getByTestId(`budget-view-${CURRENT_BUDGET_ID}`).getByRole('button').click();

  await expect(page).toHaveURL(new RegExp(`/budgets/${CURRENT_BUDGET_ID}$`));
  await expect(page.getByTestId('budget-year')).toHaveText(`${CURRENT_YEAR} Budget`);
});

test('creates next year’s budget from the list and opens it', async ({ page }) => {
  const createdYears = await setupMocks(page, [mockCurrentBudgetListItem]);

  await page.goto('/budgets');

  const createButton = page.getByTestId('create-budget-button');
  await expect(createButton).toContainText(`Create ${CURRENT_YEAR + 1} Budget`);
  await createButton.getByRole('button').click();

  await expect(page).toHaveURL(new RegExp(`/budgets/${CREATED_BUDGET_ID}$`));
  expect(createdYears).toEqual([CURRENT_YEAR + 1]);
});

test('creates the current year’s budget from the empty state', async ({ page }) => {
  const createdYears = await setupMocks(page, []);

  await page.goto('/budgets');

  await expect(page.getByTestId('budget-list-empty-state')).toBeVisible();
  await page.getByTestId('empty-create-budget-button').getByRole('button').click();

  await expect(page).toHaveURL(new RegExp(`/budgets/${CREATED_BUDGET_ID}$`));
  expect(createdYears).toEqual([CURRENT_YEAR]);
});
