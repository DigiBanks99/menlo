import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetItemApiService, BudgetSummary, CategorySummary } from 'data-access-menlo-api';
import { failure, networkError, success } from 'shared-util';
import { BudgetSummaryComponent } from './budget-summary.component';

function mockCategorySummary(overrides: Partial<CategorySummary> = {}): CategorySummary {
  return {
    id: 'cat-1',
    name: 'Salary',
    plannedTotal: 50000,
    realizedTotal: null,
    spentTotal: null,
    children: [],
    ...overrides,
  };
}

function mockBudgetSummary(overrides: Partial<BudgetSummary> = {}): BudgetSummary {
  return {
    budgetId: 'budget-1',
    year: 2025,
    month: 1,
    income: [
      mockCategorySummary({
        id: 'inc-1',
        name: 'Employment',
        plannedTotal: 50000,
        children: [
          mockCategorySummary({ id: 'inc-1-1', name: 'Salary', plannedTotal: 45000 }),
          mockCategorySummary({ id: 'inc-1-2', name: 'Bonus', plannedTotal: 5000 }),
        ],
      }),
    ],
    expenses: [
      mockCategorySummary({
        id: 'exp-1',
        name: 'Housing',
        plannedTotal: 20000,
        children: [
          mockCategorySummary({ id: 'exp-1-1', name: 'Mortgage', plannedTotal: 15000 }),
          mockCategorySummary({ id: 'exp-1-2', name: 'Utilities', plannedTotal: 5000 }),
        ],
      }),
    ],
    netPlanned: 30000,
    netRealized: null,
    netSpent: null,
    ...overrides,
  };
}

describe('BudgetSummaryComponent', () => {
  let mockApi: { getSummary: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockApi = { getSummary: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [BudgetSummaryComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetItemApiService, useValue: mockApi },
      ],
    }).compileComponents();
  });

  function createComponent() {
    const fixture = TestBed.createComponent(BudgetSummaryComponent);
    fixture.componentRef.setInput('budgetId', 'budget-1');
    fixture.detectChanges();
    return fixture;
  }

  it('should render loading state when loading', () => {
    const fixture = createComponent();
    fixture.componentInstance.loading.set(true);
    fixture.detectChanges();

    const loading = fixture.nativeElement.querySelector('.loading');
    expect(loading).toBeTruthy();
    expect(loading.textContent).toContain('Loading summary...');
  });

  it('should render error state when API fails', () => {
    mockApi.getSummary.mockReturnValue(of(failure(networkError('Server error'))));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('.error');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toBeTruthy();
  });

  it('should render income section with categories', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary())));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const incomeSection = fixture.nativeElement.querySelector('.income-section');
    expect(incomeSection).toBeTruthy();
    expect(incomeSection.textContent).toContain('Employment');
    expect(incomeSection.textContent).toContain('50,000.00');
  });

  it('should render expenses section with categories', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary())));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const expensesSection = fixture.nativeElement.querySelector('.expenses-section');
    expect(expensesSection).toBeTruthy();
    expect(expensesSection.textContent).toContain('Housing');
    expect(expensesSection.textContent).toContain('20,000.00');
  });

  it('should render net section with correct values', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary())));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const netSection = fixture.nativeElement.querySelector('.net-section');
    expect(netSection).toBeTruthy();
    expect(netSection.textContent).toContain('Net (Income - Expenses)');
    expect(netSection.textContent).toContain('30,000.00');
  });

  it('should expand/collapse categories on click', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary())));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    // Initially children should not be visible
    let children = fixture.nativeElement.querySelectorAll('.category-row.child');
    expect(children.length).toBe(0);

    // Click to expand
    const rootRow = fixture.nativeElement.querySelector('.income-section .category-row.root');
    rootRow.click();
    fixture.detectChanges();

    children = fixture.nativeElement.querySelectorAll('.income-section .category-row.child');
    expect(children.length).toBe(2);
    expect(children[0].textContent).toContain('Salary');
    expect(children[1].textContent).toContain('Bonus');

    // Click to collapse
    rootRow.click();
    fixture.detectChanges();

    children = fixture.nativeElement.querySelectorAll('.income-section .category-row.child');
    expect(children.length).toBe(0);
  });

  it('should hide realized column when no realized amounts exist', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary({ netRealized: null }))));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const realizedCells = fixture.nativeElement.querySelectorAll('.realized');
    expect(realizedCells.length).toBe(0);
  });

  it('should hide spent column when no spent amounts exist', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary({ netSpent: null }))));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const spentCells = fixture.nativeElement.querySelectorAll('.spent');
    expect(spentCells.length).toBe(0);
  });

  it('should show realized column when realized amounts exist', () => {
    const summary = mockBudgetSummary({
      netRealized: 28000,
      income: [
        mockCategorySummary({
          id: 'inc-1',
          name: 'Employment',
          plannedTotal: 50000,
          realizedTotal: 48000,
          children: [],
        }),
      ],
      expenses: [
        mockCategorySummary({
          id: 'exp-1',
          name: 'Housing',
          plannedTotal: 20000,
          realizedTotal: 20000,
          children: [],
        }),
      ],
    });
    mockApi.getSummary.mockReturnValue(of(success(summary)));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const realizedCells = fixture.nativeElement.querySelectorAll('.realized');
    expect(realizedCells.length).toBeGreaterThan(0);
  });

  it('should show spent column when spent amounts exist', () => {
    const summary = mockBudgetSummary({
      netSpent: 19000,
      income: [
        mockCategorySummary({
          id: 'inc-1',
          name: 'Employment',
          plannedTotal: 50000,
          spentTotal: 50000,
          children: [],
        }),
      ],
      expenses: [
        mockCategorySummary({
          id: 'exp-1',
          name: 'Housing',
          plannedTotal: 20000,
          spentTotal: 31000,
          children: [],
        }),
      ],
    });
    mockApi.getSummary.mockReturnValue(of(success(summary)));

    const fixture = createComponent();
    fixture.componentInstance.loadSummary();
    fixture.detectChanges();

    const spentCells = fixture.nativeElement.querySelectorAll('.spent');
    expect(spentCells.length).toBeGreaterThan(0);
  });

  it('should display yearly/monthly toggle buttons', () => {
    const fixture = createComponent();
    const buttons = fixture.nativeElement.querySelectorAll('.toggle-btn');
    expect(buttons.length).toBe(2);
    expect(buttons[0].textContent).toContain('Yearly');
    expect(buttons[1].textContent).toContain('Monthly');
  });

  it('should default to monthly view mode', () => {
    const fixture = createComponent();
    expect(fixture.componentInstance.viewMode()).toBe('monthly');
    const monthlyBtn = fixture.nativeElement.querySelectorAll('.toggle-btn')[1];
    expect(monthlyBtn.classList.contains('active')).toBe(true);
  });

  it('should switch to yearly mode and call API without month', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary({ month: null }))));

    const fixture = createComponent();
    fixture.componentInstance.setViewMode('yearly');
    fixture.detectChanges();

    expect(fixture.componentInstance.viewMode()).toBe('yearly');
    expect(mockApi.getSummary).toHaveBeenCalledWith('budget-1', undefined);
  });

  it('should show month navigation in monthly mode', () => {
    const fixture = createComponent();
    const nav = fixture.nativeElement.querySelector('.month-nav');
    expect(nav).toBeTruthy();
    expect(nav.textContent).toContain('Previous Month');
    expect(nav.textContent).toContain('Next Month');
  });

  it('should hide month navigation in yearly mode', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary({ month: null }))));

    const fixture = createComponent();
    fixture.componentInstance.setViewMode('yearly');
    fixture.detectChanges();

    const nav = fixture.nativeElement.querySelector('.month-nav');
    expect(nav).toBeNull();
  });

  it('should navigate to next month', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary())));

    const fixture = createComponent();
    fixture.componentInstance.currentMonth.set(5);
    fixture.componentInstance.nextMonth();

    expect(fixture.componentInstance.currentMonth()).toBe(6);
    expect(mockApi.getSummary).toHaveBeenCalledWith('budget-1', 6);
  });

  it('should navigate to previous month', () => {
    mockApi.getSummary.mockReturnValue(of(success(mockBudgetSummary())));

    const fixture = createComponent();
    fixture.componentInstance.currentMonth.set(5);
    fixture.componentInstance.previousMonth();

    expect(fixture.componentInstance.currentMonth()).toBe(4);
    expect(mockApi.getSummary).toHaveBeenCalledWith('budget-1', 4);
  });

  it('should not go below month 1', () => {
    const fixture = createComponent();
    fixture.componentInstance.currentMonth.set(1);
    fixture.componentInstance.previousMonth();

    expect(fixture.componentInstance.currentMonth()).toBe(1);
  });

  it('should not go above month 12', () => {
    const fixture = createComponent();
    fixture.componentInstance.currentMonth.set(12);
    fixture.componentInstance.nextMonth();

    expect(fixture.componentInstance.currentMonth()).toBe(12);
  });

  it('should display correct month label', () => {
    const fixture = createComponent();
    fixture.componentInstance.currentMonth.set(4);
    fixture.detectChanges();

    const label = fixture.nativeElement.querySelector('.current-month');
    expect(label.textContent).toContain('April');
  });
});
