import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiError, Result, failure, success, unknownError } from 'shared-util';
import { BudgetApiService, BudgetCategoryResponse, BudgetItemApiService, BudgetResponse } from 'data-access-menlo-api';
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
  let mockBudgetItemApiService: {
    getSummary: ReturnType<typeof vi.fn>;
    listItems: ReturnType<typeof vi.fn>;
  };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };
  let routeBudgetId: string | null;

  beforeEach(async () => {
    mockBudgetApiService = {
      getBudget: vi.fn(),
      createOrEnsureBudget: vi.fn(),
    };
    // Provide a never-resolving observable so the summary component doesn't interfere
    mockBudgetItemApiService = {
      getSummary: vi.fn().mockReturnValue(new Subject().asObservable()),
      listItems: vi.fn().mockReturnValue(new Subject().asObservable()),
    };
    mockRouter = { navigate: vi.fn() };
    routeBudgetId = 'budget-current';

    await TestBed.configureTestingModule({
      imports: [BudgetDetailComponent],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => routeBudgetId } } },
        },
        { provide: BudgetApiService, useValue: mockBudgetApiService },
        { provide: BudgetItemApiService, useValue: mockBudgetItemApiService },
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

    it('uses an empty id when the route parameter is missing', () => {
      routeBudgetId = null;
      mockBudgetApiService.getBudget.mockReturnValue(of(success(mockBudgetCurrentYear)));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      expect(mockBudgetApiService.getBudget).toHaveBeenCalledWith('');
    });
  });

  describe('successful budget load', () => {
    it('returns no sorted categories before a budget has loaded', () => {
      const fixture = TestBed.createComponent(BudgetDetailComponent);

      expect(fixture.componentInstance.sortedCategories()).toEqual([]);
    });

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

    it.each([
      ['Active', 'success'],
      ['Closed', 'error'],
      ['Draft', 'neutral'],
    ] satisfies readonly [BudgetResponse['status'], string][])(
      'maps %s budgets to the expected detail badge variant',
      (status, variant) => {
        const fixture = TestBed.createComponent(BudgetDetailComponent);

        expect(
          (
            fixture.componentInstance as unknown as {
              statusVariantFor(status: BudgetResponse['status']): string;
            }
          ).statusVariantFor(status),
        ).toBe(variant);
      },
    );

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

    it('renders the budget summary component when budget loads', () => {
      mockBudgetApiService.getBudget.mockReturnValue(of(success(mockBudgetCurrentYear)));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const summaryEl = fixture.nativeElement.querySelector('[data-testid="budget-summary"]');
      expect(summaryEl).toBeTruthy();
    });
  });

  describe('error handling', () => {
    it('uses an empty string when the route id is missing', () => {
      routeBudgetId = null;
      mockBudgetApiService.getBudget.mockReturnValue(of(failure(unknownError('Missing id'))));

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      expect(mockBudgetApiService.getBudget).toHaveBeenCalledWith('');
      expect(fixture.nativeElement.querySelector('[data-testid="error-banner"]')).toBeTruthy();
    });

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

    it('renders duplicate category ids only once', () => {
      const categories: BudgetCategoryResponse[] = [
        makeCat('dup', 'First Duplicate'),
        makeCat('dup', 'Second Duplicate'),
      ];

      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const items = fixture.nativeElement.querySelectorAll(
        '.category-item',
      ) as NodeListOf<HTMLElement>;
      expect(items.length).toBe(1);
      expect(items[0].querySelector('.category-name')?.textContent?.trim()).toBe('First Duplicate');
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

  describe('budget items workspace', () => {
    it('does not render workspace section by default', () => {
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories: [makeCat('cat-1', 'Housing')] })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      expect(
        fixture.nativeElement.querySelector('[data-testid="items-workspace-section"]'),
      ).toBeNull();
    });

    it('renders a View Items button for leaf categories', () => {
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories: [makeCat('cat-1', 'Housing')] })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector('[data-testid="btn-view-items-cat-1"]');
      expect(btn).toBeTruthy();
    });

    it('does not render a View Items button for parent (non-leaf) categories', () => {
      const categories = [makeCat('parent', 'Parent'), makeCat('child', 'Child', 'parent')];
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      expect(
        fixture.nativeElement.querySelector('[data-testid="btn-view-items-parent"]'),
      ).toBeNull();
      expect(
        fixture.nativeElement.querySelector('[data-testid="btn-view-items-child"]'),
      ).toBeTruthy();
    });

    it('does not open workspace when selectCategory is called for a non-leaf category', () => {
      const categories = [makeCat('parent', 'Parent'), makeCat('child', 'Child', 'parent')];
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      fixture.componentInstance.selectCategory('parent', 'Parent');
      fixture.detectChanges();

      expect(fixture.componentInstance.selectedCategoryId()).toBeNull();
      expect(
        fixture.nativeElement.querySelector('[data-testid="items-workspace-section"]'),
      ).toBeNull();
    });

    it('renders the workspace when selectCategory is called for a leaf category', () => {
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories: [makeCat('cat-1', 'Housing')] })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      fixture.componentInstance.selectCategory('cat-1', 'Housing');
      fixture.detectChanges();

      expect(
        fixture.nativeElement.querySelector('[data-testid="items-workspace-section"]'),
      ).toBeTruthy();
    });

    it('hides the workspace when the same category is selected again (toggle off)', () => {
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories: [makeCat('cat-1', 'Housing')] })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      fixture.componentInstance.selectCategory('cat-1', 'Housing');
      fixture.detectChanges();
      fixture.componentInstance.selectCategory('cat-1', 'Housing');
      fixture.detectChanges();

      expect(
        fixture.nativeElement.querySelector('[data-testid="items-workspace-section"]'),
      ).toBeNull();
    });

    it('clicking View Items button in the DOM shows the workspace', () => {
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories: [makeCat('cat-1', 'Housing')] })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector(
        '[data-testid="btn-view-items-cat-1"]',
      ) as HTMLButtonElement;
      btn.click();
      fixture.detectChanges();

      expect(
        fixture.nativeElement.querySelector('[data-testid="items-workspace-section"]'),
      ).toBeTruthy();
    });

    it('switches workspace to a different category without hiding it', () => {
      const categories = [makeCat('cat-1', 'Housing'), makeCat('cat-2', 'Food')];
      mockBudgetApiService.getBudget.mockReturnValue(
        of(success({ ...mockBudgetCurrentYear, categories })),
      );

      const fixture = TestBed.createComponent(BudgetDetailComponent);
      fixture.detectChanges();

      fixture.componentInstance.selectCategory('cat-1', 'Housing');
      fixture.detectChanges();
      fixture.componentInstance.selectCategory('cat-2', 'Food');
      fixture.detectChanges();

      expect(fixture.componentInstance.selectedCategoryId()).toBe('cat-2');
      expect(fixture.componentInstance.selectedCategoryName()).toBe('Food');
      expect(
        fixture.nativeElement.querySelector('[data-testid="items-workspace-section"]'),
      ).toBeTruthy();
    });
  });

  describe('isLeafCategory', () => {
    it('returns true for a category with no children', () => {
      const component = TestBed.createComponent(BudgetDetailComponent).componentInstance;
      const cats = [makeCat('root', 'Root'), makeCat('leaf', 'Leaf', 'root')];

      expect(component.isLeafCategory('leaf', cats)).toBe(true);
    });

    it('returns false for a category that is a parent of another', () => {
      const component = TestBed.createComponent(BudgetDetailComponent).componentInstance;
      const cats = [makeCat('root', 'Root'), makeCat('leaf', 'Leaf', 'root')];

      expect(component.isLeafCategory('root', cats)).toBe(false);
    });

    it('returns true when the category list is empty', () => {
      const component = TestBed.createComponent(BudgetDetailComponent).componentInstance;

      expect(component.isLeafCategory('any-id', [])).toBe(true);
    });
  });
});
