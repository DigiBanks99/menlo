import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiError, Result, failure, success, unknownError } from 'shared-util';
import { BudgetApiService, BudgetCategoryResponse, BudgetResponse } from 'data-access-menlo-api';
import { BudgetDetailComponent } from './budget-detail.component';

const currentYear = new Date().getFullYear();
const nextYear = currentYear + 1;

function makeCat(id: string, name: string, parentId: string | null = null): BudgetCategoryResponse {
  return {
    id,
    name,
    parentId,
    plannedMonthlyAmount: { amount: 1000, currency: 'ZAR' },
  };
}

const mockBudgetCurrentYear: BudgetResponse = {
  id: 'budget-current',
  year: currentYear,
  householdId: 'household-1',
  status: 'Draft',
  categories: [],
  totalPlannedMonthlyAmount: { amount: 0, currency: 'ZAR' },
};

const mockBudgetNextYear: BudgetResponse = {
  id: 'budget-next',
  year: nextYear,
  householdId: 'household-1',
  status: 'Draft',
  categories: [],
  totalPlannedMonthlyAmount: { amount: 0, currency: 'ZAR' },
};

describe('BudgetDetailComponent', () => {
  let mockBudgetApiService: {
    getBudget: ReturnType<typeof vi.fn>;
    createOrEnsureBudget: ReturnType<typeof vi.fn>;
  };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockBudgetApiService = {
      getBudget: vi.fn(),
      createOrEnsureBudget: vi.fn(),
    };
    mockRouter = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [BudgetDetailComponent],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'budget-current' } } },
        },
        { provide: BudgetApiService, useValue: mockBudgetApiService },
        { provide: Router, useValue: mockRouter },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('initial loading state', () => {
    it('shows loading indicator while request is in flight', () => {
      const subject = new Subject<Result<BudgetResponse, ApiError>>();
      mockBudgetApiService.getBudget.mockReturnValue(subject.asObservable());

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const loadingEl = fixture.nativeElement.querySelector('[data-testid="loading"]');
      expect(loadingEl).toBeTruthy();

      subject.next(success(mockBudgetCurrentYear));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="loading"]')).toBeNull();
    });
  });

  describe('successful budget load', () => {
    it('renders the budget year in the heading', () => {
      mockBudgetApiService.getBudget.mockReturnValue(of(success(mockBudgetCurrentYear)));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const yearEl = fixture.nativeElement.querySelector(
        '[data-testid="budget-year"]',
      ) as HTMLElement;
      expect(yearEl.textContent?.trim()).toContain(String(currentYear));
    });

    it('renders the budget status badge', () => {
      mockBudgetApiService.getBudget.mockReturnValue(of(success(mockBudgetCurrentYear)));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const badgeEl = fixture.nativeElement.querySelector(
        '[data-testid="status-badge"]',
      ) as HTMLElement;
      expect(badgeEl.textContent?.trim()).toBe('Draft');
    });

    it('renders total planned monthly amount', () => {
      mockBudgetApiService.getBudget.mockReturnValue(
        of(
          success({
            ...mockBudgetCurrentYear,
            totalPlannedMonthlyAmount: { amount: 5000, currency: 'ZAR' },
          }),
        ),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const totalEl = fixture.nativeElement.querySelector(
        '[data-testid="total-amount"]',
      ) as HTMLElement;
      expect(totalEl).toBeTruthy();
      expect(totalEl.textContent?.trim()).not.toBe('');
    });
  });

  describe('error handling', () => {
    it('shows error banner when getBudget fails', () => {
      mockBudgetApiService.getBudget.mockReturnValue(of(failure(unknownError('Not found'))));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector(
        '[data-testid="error-banner"]',
      ) as HTMLElement;
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent?.trim()).toBe('Not found');
    });

    it('does not show budget content on error', () => {
      mockBudgetApiService.getBudget.mockReturnValue(of(failure(unknownError('Error'))));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="budget-year"]')).toBeNull();
    });
  });

  describe('showCreateNextYear', () => {
    it('shows Create Next Year button when budget is for the current year', () => {
      mockBudgetApiService.getBudget.mockReturnValue(of(success(mockBudgetCurrentYear)));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector('[data-testid="create-next-year-btn"]');
      expect(btn).toBeTruthy();
      expect((btn as HTMLButtonElement).textContent?.trim()).toContain(String(nextYear));
    });

    it('hides Create Next Year button when budget is not for the current year', () => {
      mockBudgetApiService.getBudget.mockReturnValue(of(success(mockBudgetNextYear)));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      expect(
        fixture.nativeElement.querySelector('[data-testid="create-next-year-btn"]'),
      ).toBeNull();
    });
  });

  describe('createNextYearBudget', () => {
    beforeEach(() => {
      mockBudgetApiService.getBudget.mockReturnValue(of(success(mockBudgetCurrentYear)));
    });

    it('navigates to the new budget on success', () => {
      mockBudgetApiService.createOrEnsureBudget.mockReturnValue(of(success(mockBudgetNextYear)));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      fixture.componentInstance.createNextYearBudget();

      expect(mockBudgetApiService.createOrEnsureBudget).toHaveBeenCalledWith(nextYear);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/budgets', 'budget-next']);
    });

    it('shows error banner when create next year fails', () => {
      mockBudgetApiService.createOrEnsureBudget.mockReturnValue(
        of(failure(unknownError('Clone failed'))),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      fixture.componentInstance.createNextYearBudget();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector(
        '[data-testid="create-next-year-error"]',
      ) as HTMLElement;
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent?.trim()).toBe('Clone failed');
    });

    it('shows loading state during create next year request', () => {
      const subject = new Subject<Result<BudgetResponse, ApiError>>();
      mockBudgetApiService.createOrEnsureBudget.mockReturnValue(subject.asObservable());

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      fixture.componentInstance.createNextYearBudget();
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector(
        '[data-testid="create-next-year-btn"]',
      ) as HTMLButtonElement;
      expect(btn.disabled).toBe(true);
      expect(btn.textContent?.trim()).toContain('Creating...');

      subject.next(success(mockBudgetNextYear));
      fixture.detectChanges();

      expect(mockRouter.navigate).toHaveBeenCalled();
    });
  });

  describe('category rendering', () => {
    it('renders categories in topological order (parents before children)', () => {
      const categories: BudgetCategoryResponse[] = [
        makeCat('child1', 'Child One', 'root1'),
        makeCat('root1', 'Root One'),
      ];

      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const items = fixture.nativeElement.querySelectorAll(
        '.category-item',
      ) as NodeListOf<HTMLElement>;
      expect(items.length).toBe(2);
      // Root should appear first
      expect(items[0].querySelector('.category-name')?.textContent?.trim()).toBe('Root One');
      expect(items[1].querySelector('.category-name')?.textContent?.trim()).toBe('Child One');
    });

    it('renders orphaned categories (parent not in list) at the end', () => {
      const categories: BudgetCategoryResponse[] = [
        makeCat('root1', 'Root One'),
        makeCat('orphan1', 'Orphan One', 'nonexistent-parent'),
      ];

      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const items = fixture.nativeElement.querySelectorAll(
        '.category-item',
      ) as NodeListOf<HTMLElement>;
      expect(items.length).toBe(2);
      expect(items[1].querySelector('.category-name')?.textContent?.trim()).toBe('Orphan One');
    });

    it('shows depth warning for categories at depth >= 4', () => {
      // depth 4 requires a chain: root → d1 → d2 → d3 → d4 (d4 is at depth 4)
      const categories: BudgetCategoryResponse[] = [
        makeCat('root', 'Root'),
        makeCat('d1', 'Depth 1', 'root'),
        makeCat('d2', 'Depth 2', 'd1'),
        makeCat('d3', 'Depth 3', 'd2'),
        makeCat('d4', 'Depth 4', 'd3'),
      ];

      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const warnings = fixture.nativeElement.querySelectorAll('[data-testid="depth-warning"]');
      expect(warnings.length).toBe(1);
    });

    it('does not show depth warning for categories at depth < 4', () => {
      const categories: BudgetCategoryResponse[] = [
        makeCat('root', 'Root'),
        makeCat('child', 'Child', 'root'),
      ];

      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const warnings = fixture.nativeElement.querySelectorAll('[data-testid="depth-warning"]');
      expect(warnings.length).toBe(0);
    });
  });

  describe('getDepth', () => {
    it('returns 0 for a root category (no parentId)', () => {
      const component = TestBed.createComponent(BudgetDetailComponent).componentInstance;
      const root = makeCat('root', 'Root');

      expect(component.getDepth(root, [root])).toBe(0);
    });

    it('returns correct depth for a nested category', () => {
      const component = TestBed.createComponent(BudgetDetailComponent).componentInstance;
      const root = makeCat('root', 'Root');
      const child = makeCat('child', 'Child', 'root');
      const grandchild = makeCat('gc', 'GrandChild', 'child');

      expect(component.getDepth(grandchild, [root, child, grandchild])).toBe(2);
    });

    it('stops counting when parent is not found in the list (orphan)', () => {
      const component = TestBed.createComponent(BudgetDetailComponent).componentInstance;
      const orphan = makeCat('orphan', 'Orphan', 'nonexistent');

      // depth should stop at 1 attempt since parent not found
      expect(component.getDepth(orphan, [orphan])).toBe(0);
    });
  });
});
