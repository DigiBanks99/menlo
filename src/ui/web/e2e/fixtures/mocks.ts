import { Page } from '@playwright/test';

export const mockUser = {
  id: 'user-e2e-001',
  email: 'test@menlo.local',
  displayName: 'E2E Test User',
  roles: ['Menlo.User'],
  onboarding: {
    isComplete: true,
    pendingTasks: [],
  },
};

/** BFF-style auth: the app initializer calls this before rendering any route. */
export async function mockAuthUser(page: Page): Promise<void> {
  await page.route('**/auth/user', (route) => route.fulfill({ json: mockUser }));
}

export function budgetDetail(
  id: string,
  year: number,
  status: string,
  categories: { id: string; name: string; parentId: string | null }[] = [],
) {
  return {
    id,
    year,
    householdId: 'hh-e2e-001',
    status,
    categories,
    totalPlannedMonthlyAmount: { amount: 0, currency: 'ZAR' },
  };
}

export function emptySummary(budgetId: string, year: number) {
  return {
    budgetId,
    year,
    month: null,
    income: [],
    expenses: [],
    netPlanned: 0,
    netRealized: null,
    netSpent: null,
  };
}
