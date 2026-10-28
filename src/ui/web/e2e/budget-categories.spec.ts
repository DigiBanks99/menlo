import { expect, Page, test } from '@playwright/test';
import { budgetDetail, emptySummary, mockAuthUser } from './fixtures/mocks';

const BUDGET_ID = 'budget-e2e-categories-001';
const YEAR = 2025;

interface StoredCategory {
  id: string;
  name: string;
  description: string;
  budgetFlow: string;
  isDeleted: boolean;
  parentId: string | null;
}

function toTree(
  store: StoredCategory[],
  includeDeleted: boolean,
  parentId: string | null = null,
): unknown[] {
  return store
    .filter((c) => c.parentId === parentId && (includeDeleted || !c.isDeleted))
    .map((c) => ({
      id: c.id,
      name: c.name,
      description: c.description,
      budgetFlow: c.budgetFlow,
      isDeleted: c.isDeleted,
      children: toTree(store, includeDeleted, c.id),
    }));
}

/** Mocks the detail page reads plus an in-memory category store behind the category endpoints. */
async function setupMocks(page: Page, store: StoredCategory[]): Promise<void> {
  await mockAuthUser(page);

  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}$`), (route) =>
    route.fulfill({ json: budgetDetail(BUDGET_ID, YEAR, 'Active') }),
  );
  await page.route(new RegExp(`/api/budgets/${BUDGET_ID}/summary`), (route) =>
    route.fulfill({ json: emptySummary(BUDGET_ID, YEAR) }),
  );

  let nextId = 1;
  const categoryRoute = new RegExp(
    `/api/budgets/${BUDGET_ID}/categories(?:/(?<id>[^/?]+)(?:/(?<action>restore))?)?(?:\\?.*)?$`,
  );
  await page.route(categoryRoute, (route) => {
    const request = route.request();
    const url = new URL(request.url());
    const { id: categoryId, action } = categoryRoute.exec(request.url())?.groups ?? {};
    const existing = store.find((c) => c.id === categoryId);

    switch (request.method()) {
      case 'GET': {
        const includeDeleted = url.searchParams.get('includeDeleted') === 'true';
        return route.fulfill({ json: toTree(store, includeDeleted) });
      }
      case 'POST': {
        const body = request.postDataJSON();
        const created: StoredCategory = {
          id: `cat-e2e-new-${nextId++}`,
          name: body.name,
          description: body.description ?? '',
          budgetFlow: body.budgetFlow,
          isDeleted: false,
          parentId: body.parentId ?? null,
        };
        store.push(created);
        return route.fulfill({ status: 201, json: { ...created, budgetId: BUDGET_ID } });
      }
      case 'DELETE':
        if (existing) existing.isDeleted = true;
        return route.fulfill({ status: 204 });
      case 'PUT':
        if (existing && action === 'restore') {
          existing.isDeleted = false;
          return route.fulfill({ json: { ...existing, budgetId: BUDGET_ID } });
        }
        return route.fulfill({ status: 404 });
      default:
        return route.continue();
    }
  });
}

const seed = (): StoredCategory[] => [
  {
    id: 'cat-e2e-food',
    name: 'Food',
    description: '',
    budgetFlow: 'Expense',
    isDeleted: false,
    parentId: null,
  },
  {
    id: 'cat-e2e-groceries',
    name: 'Groceries',
    description: '',
    budgetFlow: 'Expense',
    isDeleted: false,
    parentId: 'cat-e2e-food',
  },
  {
    id: 'cat-e2e-salary',
    name: 'Salary',
    description: '',
    budgetFlow: 'Income',
    isDeleted: false,
    parentId: null,
  },
];

test.describe('budget categories', () => {
  test('lists the category tree and expands a parent to reveal its children', async ({ page }) => {
    await setupMocks(page, seed());
    await page.goto(`/budgets/${BUDGET_ID}`);

    const tree = page.getByTestId('tree-list');
    await expect(tree.getByTestId('tree-node-cat-e2e-food')).toBeVisible();
    await expect(tree.getByTestId('tree-node-cat-e2e-salary')).toBeVisible();
    await expect(page.getByTestId('tree-node-cat-e2e-groceries')).toHaveCount(0);

    await page.getByTestId('toggle-cat-e2e-food').click();
    await expect(page.getByTestId('tree-node-cat-e2e-groceries')).toBeVisible();
  });

  test('adds a root category and a child category', async ({ page }) => {
    await setupMocks(page, seed());
    await page.goto(`/budgets/${BUDGET_ID}`);

    await page.getByTestId('btn-add-root').click();
    await expect(page.getByTestId('category-form')).toBeVisible();
    await page.getByTestId('input-name').fill('Utilities');
    await page.getByTestId('select-budgetFlow').selectOption('Expense');
    await page.getByTestId('btn-save').click();

    const utilities = page.getByTestId('tree-node-cat-e2e-new-1');
    await expect(utilities).toContainText('Utilities');
    await expect(page.getByTestId('category-form')).toHaveCount(0);

    await page.getByTestId('btn-add-child-cat-e2e-new-1').click();
    await page.getByTestId('input-name').fill('Electricity');
    await page.getByTestId('select-budgetFlow').selectOption('Expense');
    await page.getByTestId('btn-save').click();

    await expect(page.getByTestId('category-form')).toHaveCount(0);
    await page.getByTestId('toggle-cat-e2e-new-1').click();
    await expect(page.getByTestId('tree-node-cat-e2e-new-2')).toContainText('Electricity');
  });

  test('requires a name before a category can be saved', async ({ page }) => {
    await setupMocks(page, seed());
    await page.goto(`/budgets/${BUDGET_ID}`);

    await page.getByTestId('btn-add-root').click();
    await page.getByTestId('btn-save').click();

    await expect(page.getByTestId('error-name')).toBeVisible();
    await expect(page.getByTestId('category-form')).toBeVisible();
  });

  test('deletes a category and restores it from the deleted view', async ({ page }) => {
    await setupMocks(page, seed());
    await page.goto(`/budgets/${BUDGET_ID}`);

    await page.getByTestId('btn-delete-cat-e2e-salary').click();
    await expect(page.getByTestId('tree-node-cat-e2e-salary')).toHaveCount(0);

    await page.getByTestId('toggle-include-deleted').check();
    const deleted = page.getByTestId('tree-node-cat-e2e-salary');
    await expect(deleted).toBeVisible();

    await page.getByTestId('btn-restore-cat-e2e-salary').click();
    await page.getByTestId('toggle-include-deleted').uncheck();
    await expect(page.getByTestId('tree-node-cat-e2e-salary')).toBeVisible();
    await expect(page.getByTestId('btn-delete-cat-e2e-salary')).toBeVisible();
  });

  test('filters the tree by search text', async ({ page }) => {
    // The two-column detail layout squeezes the search input to zero width at 1280px.
    await page.setViewportSize({ width: 1920, height: 1080 });
    await setupMocks(page, seed());
    await page.goto(`/budgets/${BUDGET_ID}`);

    await page.getByTestId('filter-search').fill('sal');

    await expect(page.getByTestId('tree-node-cat-e2e-salary')).toBeVisible();
    await expect(page.getByTestId('tree-node-cat-e2e-food')).toHaveCount(0);
  });
});
