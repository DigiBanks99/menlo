import { expect, Page, test } from '@playwright/test';

// ─── Test fixture data ───────────────────────────────────────────────────────

const BUDGET_ID = 'budget-e2e-create-001';
const PARENT_CAT_ID = 'cat-e2e-food';
const LEAF_CAT_ID = 'cat-e2e-groceries';
const NEW_ITEM_ID = 'item-e2e-created-001';

const mockUser = {
  id: 'user-e2e-001',
  email: 'test@menlo.local',
  displayName: 'E2E Test User',
  roles: ['Menlo.User'],
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

/** Category tree response for the CategoryTreeComponent */
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

const mockCreatedItem = {
  id: NEW_ITEM_ID,
  budgetId: BUDGET_ID,
  categoryId: LEAF_CAT_ID,
  month: 3,
  budgetFlow: 'Expense',
  plannedAmount: 1000,
  plannedCurrency: 'ZAR',
  realizedAmount: null,
  realizedCurrency: null,
  spentAmount: null,
  spentCurrency: null,
  payerSplit: [{ userId: 'user-e2e-001', percent: 100 }],
  attributionSplit: [{ attribution: 'Main', percent: 100 }],
  adjustmentRuleId: null,
  isManualOverride: false,
};

// ─── Mock setup helper ────────────────────────────────────────────────────────

async function setupMocks(page: Page): Promise<void> {
  // Auth: BFF-style — app initializer calls this before rendering any route
  await page.route('**/auth/user', (route) => route.fulfill({ json: mockUser }));

  // Budget detail
  await page.route(
    new RegExp(`/api/budgets/${BUDGET_ID}$`),
    (route) => route.fulfill({ json: mockBudget }),
  );

  // Category tree (used by CategoryTreeComponent)
  await page.route(
    new RegExp(`/api/budgets/${BUDGET_ID}/categories(\\?.*)?$`),
    (route) => route.fulfill({ json: mockCategoryTree }),
  );

  // Budget summary (monthly — includes ?month=N query param)
  await page.route(
    new RegExp(`/api/budgets/${BUDGET_ID}/summary`),
    (route) => route.fulfill({ json: mockSummary }),
  );

  // Items list + single-item create — same URL, different methods
  let itemCreated = false;
  await page.route(
    new RegExp(`/api/budgets/${BUDGET_ID}/categories/${LEAF_CAT_ID}/items$`),
    (route) => {
      const method = route.request().method();
      if (method === 'GET') {
        return route.fulfill({ json: itemCreated ? [mockCreatedItem] : [] });
      }
      if (method === 'POST') {
        itemCreated = true;
        return route.fulfill({ status: 201, json: mockCreatedItem });
      }
      return route.continue();
    },
  );
}

// ─── Test ─────────────────────────────────────────────────────────────────────

test('creates a single budget item via the create form on the budget detail page', async ({
  page,
}) => {
  await setupMocks(page);

  // Navigate to the budget detail page
  await page.goto(`/budgets/${BUDGET_ID}`);
  await expect(page.getByTestId('budget-year')).toHaveText('2025 Budget');

  // Open the items workspace for the leaf category
  await page.getByTestId(`btn-view-items-${LEAF_CAT_ID}`).click();
  await expect(page.getByTestId('budget-items-workspace')).toBeVisible();

  // Workspace should start empty
  await expect(page.getByTestId('state-empty')).toBeVisible();

  // Open the single-item create panel
  await page.getByTestId('btn-open-single-create').click();
  await expect(page.getByTestId('single-create-panel')).toBeVisible();
  await expect(page.getByTestId('budget-item-form')).toBeVisible();

  // Fill the item form
  await page.getByTestId('input-month').fill('3');
  await page.getByTestId('select-budgetFlow').selectOption('Expense');
  await page.getByTestId('input-plannedAmount').fill('1000');

  // Add a payer split row (must total 100 %)
  await page.getByTestId('btn-add-payer').click();
  await page.getByTestId('input-payer-userId-0').fill('user-e2e-001');
  await page.getByTestId('input-payer-percent-0').fill('100');

  // Add an attribution split row (must total 100 %)
  await page.getByTestId('btn-add-attribution').click();
  await page.getByTestId('select-attribution-0').selectOption('Main');
  await page.getByTestId('input-attribution-percent-0').fill('100');

  // Submit the form
  await page.getByTestId('btn-save').click();

  // The create panel should close and the new item should appear in the list
  await expect(page.getByTestId('single-create-panel')).not.toBeVisible();
  await expect(page.getByTestId(`item-row-${NEW_ITEM_ID}`)).toBeVisible();

  const itemRow = page.getByTestId(`item-row-${NEW_ITEM_ID}`);
  await expect(itemRow.getByTestId('item-month')).toHaveText('Month 3');
  await expect(itemRow.getByTestId('item-flow')).toHaveText('Expense');
});
