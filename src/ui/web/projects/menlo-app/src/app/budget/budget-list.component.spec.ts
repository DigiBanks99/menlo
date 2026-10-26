import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { BudgetApiService, BudgetListItemResponse, BudgetResponse } from 'data-access-menlo-api';
import { MnlToastService } from 'menlo-lib';
import { Subject, of } from 'rxjs';
import { ApiError, Result, failure, success, unknownError } from 'shared-util';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetListComponent } from './budget-list.component';

const currentYear = new Date().getFullYear();

function budgetItem(overrides: Partial<BudgetListItemResponse> = {}): BudgetListItemResponse {
  return {
    id: 'budget-current',
    year: currentYear,
    status: 'Active',
    plannedExpenses: { amount: 10000, currency: 'ZAR' },
    spentExpenses: { amount: 5000, currency: 'ZAR' },
    categoryCount: 3,
    ...overrides,
  };
}

const createdBudget: BudgetResponse = {
  id: 'budget-created',
  year: currentYear + 1,
  householdId: 'household-1',
  status: 'Draft',
  categories: [],
  totalPlannedMonthlyAmount: { amount: 0, currency: 'ZAR' },
};

describe('BudgetListComponent', () => {
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };
  let mockBudgetApiService: {
    listBudgets: ReturnType<typeof vi.fn>;
    createOrEnsureBudget: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockRouter = { navigate: vi.fn().mockResolvedValue(true) };
    mockBudgetApiService = {
      listBudgets: vi.fn().mockReturnValue(of(success([budgetItem()]))),
      createOrEnsureBudget: vi.fn().mockReturnValue(of(success(createdBudget))),
    };

    await TestBed.configureTestingModule({
      imports: [BudgetListComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: Router, useValue: mockRouter },
        { provide: BudgetApiService, useValue: mockBudgetApiService },
      ],
    }).compileComponents();
  });

  function render(budgets?: BudgetListItemResponse[]) {
    if (budgets) {
      mockBudgetApiService.listBudgets.mockReturnValue(of(success(budgets)));
    }

    const fixture = TestBed.createComponent(BudgetListComponent);
    fixture.detectChanges();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function query<T extends HTMLElement>(element: HTMLElement, selector: string): T {
    return element.querySelector(selector) as T;
  }

  describe('loading budgets', () => {
    it('shows the loading state while the request is in flight', () => {
      const pending = new Subject<Result<BudgetListItemResponse[], ApiError>>();
      mockBudgetApiService.listBudgets.mockReturnValue(pending.asObservable());

      const { element } = render();

      expect(query(element, '[data-testid="budget-list-loading"]')).toBeTruthy();
      expect(query(element, '[data-testid="create-budget-button"]')).toBeNull();
    });

    it('renders a card per budget returned by the API', () => {
      const { element } = render([
        budgetItem({ id: 'next', year: currentYear + 1, status: 'Draft' }),
        budgetItem({ id: 'current', year: currentYear }),
      ]);

      const cards = element.querySelectorAll('[data-testid^="budget-card-"]');

      expect(mockBudgetApiService.listBudgets).toHaveBeenCalledOnce();
      expect(cards).toHaveLength(2);
      expect(cards[0]?.textContent).toContain(`Budget ${currentYear + 1}`);
      expect(cards[1]?.textContent).toContain(`Budget ${currentYear}`);
      expect(cards[1]?.textContent).toContain('3 categories');
    });

    it('shows the error state when the API call fails', () => {
      mockBudgetApiService.listBudgets.mockReturnValue(of(failure(unknownError('Boom'))));

      const { element } = render();

      const error = query(element, '[data-testid="budget-list-error"]');
      expect(error.textContent).toContain('Boom');
      expect(query(element, '[data-testid="budget-list-empty-state"]')).toBeNull();
      expect(query(element, '[data-testid="create-budget-button"]')).toBeNull();
    });

    it('shows the empty state when there are no budgets', () => {
      const { element } = render([]);

      const emptyState = query(element, '[data-testid="budget-list-empty-state"]');
      expect(emptyState.textContent).toContain('No budgets yet');
      expect(emptyState.textContent).toContain('Create Your First Budget');
      expect(query(element, '[data-testid="create-budget-button"]')).toBeNull();
    });
  });

  describe('budget cards', () => {
    it('shows this month’s spending against plan with a success variant', () => {
      const { element } = render([budgetItem({ id: 'b1' })]);

      const progress = query(
        element,
        '[data-testid="budget-progress-b1"] [data-testid="mnl-progress"]',
      );
      const spend = query(element, '[data-testid="budget-spend-b1"]');

      expect(progress.dataset.variant).toBe('success');
      expect(spend.textContent).toContain('50%');
      expect(spend.textContent).toContain('planned this month');
    });

    it('uses the warning variant at 80% and the error variant at 95%', () => {
      const { element } = render([
        budgetItem({ id: 'warn', spentExpenses: { amount: 8000, currency: 'ZAR' } }),
        budgetItem({ id: 'over', spentExpenses: { amount: 10200, currency: 'ZAR' } }),
      ]);

      const warn = query(
        element,
        '[data-testid="budget-progress-warn"] [data-testid="mnl-progress"]',
      );
      const over = query(
        element,
        '[data-testid="budget-progress-over"] [data-testid="mnl-progress"]',
      );

      expect(warn.dataset.variant).toBe('warning');
      expect(over.dataset.variant).toBe('error');
    });

    it('shows nothing spent at 0% and unplanned spending as fully over budget', () => {
      const { element } = render([
        budgetItem({ id: 'unspent', spentExpenses: null, categoryCount: 1 }),
        budgetItem({ id: 'unplanned', plannedExpenses: { amount: 0, currency: 'ZAR' } }),
        budgetItem({
          id: 'empty',
          plannedExpenses: { amount: 0, currency: 'ZAR' },
          spentExpenses: null,
        }),
      ]);

      expect(query(element, '[data-testid="budget-spend-empty"]').textContent).toContain('0%');
      const unspent = query(element, '[data-testid="budget-spend-unspent"]');
      const unplanned = query(element, '[data-testid="budget-spend-unplanned"]');

      expect(unspent.textContent).toContain('Nothing spent');
      expect(unspent.textContent).toContain('0%');
      expect(unplanned.textContent).toContain('100%');
      expect(
        query(element, '[data-testid="budget-progress-unplanned"] [data-testid="mnl-progress"]')
          .dataset.variant,
      ).toBe('error');
      expect(query(element, '[data-testid="budget-card-unspent"]').textContent).toContain(
        '1 category',
      );
    });

    it('maps budget status to badge variants', () => {
      const { element } = render([
        budgetItem({ id: 'active', status: 'Active' }),
        budgetItem({ id: 'draft', status: 'Draft' }),
        budgetItem({ id: 'closed', status: 'Closed' }),
      ]);

      const variantOf = (id: string) =>
        query(element, `[data-testid="budget-status-${id}"] [data-testid="mnl-badge"]`).dataset
          .variant;

      expect(variantOf('active')).toBe('success');
      expect(variantOf('draft')).toBe('warning');
      expect(variantOf('closed')).toBe('neutral');
    });

    it('navigates to the budget detail page when View Details is pressed', () => {
      const { element } = render([budgetItem({ id: 'b1' })]);

      query<HTMLButtonElement>(
        element,
        '[data-testid="budget-view-b1"] [data-testid="mnl-button"]',
      ).click();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/budgets', 'b1']);
    });
  });

  describe('next budget year', () => {
    it('is the current year when there are no budgets', () => {
      const { fixture } = render([]);

      expect(fixture.componentInstance.nextBudgetYear()).toBe(currentYear);
    });

    it('is next year when the current year exists', () => {
      const { fixture } = render([budgetItem({ year: currentYear })]);

      expect(fixture.componentInstance.nextBudgetYear()).toBe(currentYear + 1);
    });

    it('is the current year when only older budgets exist', () => {
      const { fixture } = render([budgetItem({ year: currentYear - 2 })]);

      expect(fixture.componentInstance.nextBudgetYear()).toBe(currentYear);
    });

    it('is the current year when only next year exists', () => {
      const { fixture } = render([budgetItem({ year: currentYear + 1 })]);

      expect(fixture.componentInstance.nextBudgetYear()).toBe(currentYear);
    });

    it('is null and hides the create button once current and next year exist', () => {
      const { fixture, element } = render([
        budgetItem({ id: 'next', year: currentYear + 1 }),
        budgetItem({ id: 'current', year: currentYear }),
      ]);

      expect(fixture.componentInstance.nextBudgetYear()).toBeNull();
      expect(query(element, '[data-testid="create-budget-button"]')).toBeNull();
    });
  });

  describe('creating a budget', () => {
    it('labels the button with the next year, creates it, toasts and navigates', () => {
      const { element } = render([budgetItem({ year: currentYear })]);
      const toastService = TestBed.inject(MnlToastService);
      const toastSpy = vi.spyOn(toastService, 'show');

      const button = query(element, '[data-testid="create-budget-button"]');
      expect(button.textContent).toContain(`Create ${currentYear + 1} Budget`);

      query<HTMLButtonElement>(button, '[data-testid="mnl-button"]').click();

      expect(mockBudgetApiService.createOrEnsureBudget).toHaveBeenCalledWith(currentYear + 1);
      expect(toastSpy).toHaveBeenCalledWith(`Budget ${currentYear + 1} created.`, {
        variant: 'success',
      });
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/budgets', 'budget-created']);
    });

    it('creates the current year from the empty state', () => {
      const { element } = render([]);

      query<HTMLButtonElement>(
        element,
        '[data-testid="empty-create-budget-button"] [data-testid="mnl-button"]',
      ).click();

      expect(mockBudgetApiService.createOrEnsureBudget).toHaveBeenCalledWith(currentYear);
    });

    it('shows the creating state while the request is in flight', () => {
      const pending = new Subject<Result<BudgetResponse, ApiError>>();
      mockBudgetApiService.createOrEnsureBudget.mockReturnValue(pending.asObservable());
      const { fixture, element } = render([budgetItem({ year: currentYear })]);

      query<HTMLButtonElement>(
        element,
        '[data-testid="create-budget-button"] [data-testid="mnl-button"]',
      ).click();
      fixture.detectChanges();

      expect(query(element, '[data-testid="create-budget-button"]').textContent).toContain(
        'Creating...',
      );
    });

    it('shows the empty-state creating label while the request is in flight', () => {
      const pending = new Subject<Result<BudgetResponse, ApiError>>();
      mockBudgetApiService.createOrEnsureBudget.mockReturnValue(pending.asObservable());
      const { fixture, element } = render([]);

      query<HTMLButtonElement>(
        element,
        '[data-testid="empty-create-budget-button"] [data-testid="mnl-button"]',
      ).click();
      fixture.detectChanges();

      expect(query(element, '[data-testid="empty-create-budget-button"]').textContent).toContain(
        'Creating...',
      );
    });

    it('shows an inline error and error toast when creation fails', () => {
      mockBudgetApiService.createOrEnsureBudget.mockReturnValue(
        of(failure(unknownError('Year must be 2026 or 2027.'))),
      );
      const { fixture, element } = render([budgetItem({ year: currentYear })]);
      const toastSpy = vi.spyOn(TestBed.inject(MnlToastService), 'show');

      query<HTMLButtonElement>(
        element,
        '[data-testid="create-budget-button"] [data-testid="mnl-button"]',
      ).click();
      fixture.detectChanges();

      expect(query(element, '[data-testid="create-budget-error"]').textContent).toContain(
        'Year must be 2026 or 2027.',
      );
      expect(toastSpy).toHaveBeenCalledWith('Year must be 2026 or 2027.', { variant: 'error' });
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('does nothing when there is no year left to create', () => {
      const { fixture } = render([
        budgetItem({ id: 'next', year: currentYear + 1 }),
        budgetItem({ id: 'current', year: currentYear }),
      ]);

      (fixture.componentInstance as unknown as { createBudget(): void }).createBudget();

      expect(mockBudgetApiService.createOrEnsureBudget).not.toHaveBeenCalled();
    });
  });
});
