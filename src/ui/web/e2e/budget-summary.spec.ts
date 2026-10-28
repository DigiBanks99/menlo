import { expect, Page, test } from '@playwright/test';
import { budgetDetail, mockAuthUser } from './fixtures/mocks';

const BUDGET_ID = 'budget-e2e-summary-001';
const YEAR = 2025;

const mockSummary = {
  budgetId: BUDGET_ID,
  year: YEAR,
  month: null,
  income: [
    {
      id: 'cat-income',
      name: 'Salary',
      plannedTotal: 50000,
      realizedTotal: null,
      spentTotal: null,
      children: [
        {
          id: 'cat-income-base',
          name: 'Base pay',
          plannedTotal: 50000,
          realizedTotal: null,
          spentTotal: null,
          children: [],
        },
      ],
    },
  ],
  expenses: [
    {
      id: 'cat-food',
      name: 'Food',
      plannedTotal: 12000.5,
      realizedTotal: null,
      spentTotal: null,
      children: [],
    },
  ],
  netPlanned: 37999.5,
  netRealized: null,
  netSpent: null,
};

/** Mocks the detail page reads and records the `month` query param of every summary request. */
async function setupMocks(page: Page): Promise<(string | null)[]> {
  const summaryMonths: (string | null)[] = [];

  await mockAuthUser(page);
  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}$`), (route) =>
    route.fulfill({ json: budgetDetail(BUDGET_ID, YEAR, 'Active') }),
  );
  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}/categories(\\?.*)?$`), (route) =>
    route.fulfill({ json: [] }),
  );
  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}/summary`), (route) => {
    summaryMonths.push(new URL(route.request().url()).searchParams.get('month'));
    return route.fulfill({ json: mockSummary });
  });

  return summaryMonths;
}

test.describe('budget summary', () => {
  test('shows income, expenses and net planned totals', async ({ page }) => {
    await setupMocks(page);
    await page.goto(`/budgets/${BUDGET_ID}`);

    const summary = page.getByTestId('budget-summary');
    await expect(summary.locator('.income-section .category-row')).toContainText('Salary');
    await expect(summary.locator('.income-section .planned')).toHaveText('50,000.00');
    await expect(summary.locator('.expenses-section .category-row')).toContainText('Food');
    await expect(summary.locator('.expenses-section .planned')).toHaveText('12,000.50');
    await expect(summary.locator('.net-row .planned')).toHaveText('37,999.50');
  });

  test('expands and collapses a parent category to show its children', async ({ page }) => {
    await setupMocks(page);
    await page.goto(`/budgets/${BUDGET_ID}`);

    const summary = page.getByTestId('budget-summary');
    await expect(summary.locator('.category-row.child')).toHaveCount(0);

    await summary.locator('.income-section .category-row.root').click();
    await expect(summary.locator('.category-row.child')).toContainText('Base pay');

    await summary.locator('.income-section .category-row.root').click();
    await expect(summary.locator('.category-row.child')).toHaveCount(0);
  });

  test('switches between monthly and yearly views and navigates months', async ({ page }) => {
    const summaryMonths = await setupMocks(page);
    const currentMonth = new Date().getMonth() + 1;
    await page.goto(`/budgets/${BUDGET_ID}`);

    const summary = page.getByTestId('budget-summary');
    await expect(summary.locator('.net-row')).toBeVisible();
    await expect.poll(() => summaryMonths.at(-1)).toBe(String(currentMonth));

    await summary.getByRole('button', { name: 'Yearly' }).click();
    await expect(summary.locator('.month-nav')).toHaveCount(0);
    await expect.poll(() => summaryMonths.at(-1)).toBeNull();

    await summary.getByRole('button', { name: 'Monthly' }).click();
    await expect(summary.locator('.month-nav')).toBeVisible();

    // Navigate relative to the current month; the buttons disable at the year's edges.
    const goForward = currentMonth < 12;
    const target = goForward ? currentMonth + 1 : currentMonth - 1;
    await summary
      .getByRole('button', { name: goForward ? /Next Month/ : /Previous Month/ })
      .click();
    await expect.poll(() => summaryMonths.at(-1)).toBe(String(target));
  });
});
