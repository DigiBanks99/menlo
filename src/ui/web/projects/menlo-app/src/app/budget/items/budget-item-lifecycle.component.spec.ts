import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetItemApiService, BudgetItemDto } from 'data-access-menlo-api';
import { ApiError, Result, failure, networkError, success } from 'shared-util';
import { BudgetItemLifecycleComponent } from './budget-item-lifecycle.component';

const mockBudgetId = 'budget-1';
const mockCategoryId = 'cat-1';

function mockBudgetItemDto(overrides: Partial<BudgetItemDto> = {}): BudgetItemDto {
  return {
    id: 'item-1',
    budgetId: mockBudgetId,
    categoryId: mockCategoryId,
    month: 1,
    budgetFlow: 'Expense',
    plannedAmount: 1500,
    plannedCurrency: 'ZAR',
    realizedAmount: null,
    realizedCurrency: null,
    spentAmount: null,
    spentCurrency: null,
    payerSplit: [],
    attributionSplit: [],
    adjustmentRuleId: null,
    isManualOverride: false,
    ...overrides,
  };
}

describe('BudgetItemLifecycleComponent', () => {
  let mockBudgetItemApi: {
    realizeItem: ReturnType<typeof vi.fn>;
    recordItemSpent: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockBudgetItemApi = {
      realizeItem: vi.fn(),
      recordItemSpent: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [BudgetItemLifecycleComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetItemApiService, useValue: mockBudgetItemApi },
      ],
    }).compileComponents();
  });

  function createComponent(item?: BudgetItemDto) {
    const fixture = TestBed.createComponent(BudgetItemLifecycleComponent);
    fixture.componentRef.setInput('budgetId', mockBudgetId);
    fixture.componentRef.setInput('categoryId', mockCategoryId);
    fixture.componentRef.setInput('item', item ?? mockBudgetItemDto());
    fixture.detectChanges();
    return fixture;
  }

  it('shows planned amount', () => {
    const fixture = createComponent();
    const el = fixture.nativeElement.querySelector('[data-testid="planned-amount"]');
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('ZAR');
    expect(el.textContent).toContain('1500');
  });

  it('shows realized amount when present', () => {
    const fixture = createComponent(
      mockBudgetItemDto({ realizedAmount: 1600, realizedCurrency: 'ZAR' }),
    );
    const el = fixture.nativeElement.querySelector('[data-testid="realized-amount"]');
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('1600');
  });

  it('does not show realized amount when null', () => {
    const fixture = createComponent();
    const el = fixture.nativeElement.querySelector('[data-testid="realized-amount"]');
    expect(el).toBeNull();
  });

  it('shows spent amount when present', () => {
    const fixture = createComponent(mockBudgetItemDto({ spentAmount: 1400, spentCurrency: 'ZAR' }));
    const el = fixture.nativeElement.querySelector('[data-testid="spent-amount"]');
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('1400');
  });

  it('does not show spent amount when null', () => {
    const fixture = createComponent();
    const el = fixture.nativeElement.querySelector('[data-testid="spent-amount"]');
    expect(el).toBeNull();
  });

  it('"Record Bill" button switches to realize form', () => {
    const fixture = createComponent();
    const btn = fixture.nativeElement.querySelector('[data-testid="btn-realize"]');
    btn.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('[data-testid="input-amount"]');
    expect(input).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="btn-realize"]')).toBeNull();
  });

  it('"Record Payment" button switches to spent form', () => {
    const fixture = createComponent();
    const btn = fixture.nativeElement.querySelector('[data-testid="btn-spent"]');
    btn.click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('[data-testid="input-amount"]');
    expect(input).toBeTruthy();
  });

  it('submit calls realizeItem API in realize mode', () => {
    const updatedItem = mockBudgetItemDto({ realizedAmount: 1600, realizedCurrency: 'ZAR' });
    mockBudgetItemApi.realizeItem.mockReturnValue(of(success(updatedItem)));

    const fixture = createComponent();
    const updatedSpy = vi.fn();
    fixture.componentInstance.updated.subscribe(updatedSpy);

    fixture.nativeElement.querySelector('[data-testid="btn-realize"]').click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-amount"]',
    ) as HTMLInputElement;
    input.value = '1600';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-submit"]').click();
    fixture.detectChanges();

    expect(mockBudgetItemApi.realizeItem).toHaveBeenCalledWith(
      mockBudgetId,
      mockCategoryId,
      'item-1',
      { amount: 1600, currency: 'ZAR' },
    );
    expect(updatedSpy).toHaveBeenCalledWith(updatedItem);
  });

  it('submit calls recordItemSpent API in spent mode', () => {
    const updatedItem = mockBudgetItemDto({ spentAmount: 1400, spentCurrency: 'ZAR' });
    mockBudgetItemApi.recordItemSpent.mockReturnValue(of(success(updatedItem)));

    const fixture = createComponent();
    const updatedSpy = vi.fn();
    fixture.componentInstance.updated.subscribe(updatedSpy);

    fixture.nativeElement.querySelector('[data-testid="btn-spent"]').click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-amount"]',
    ) as HTMLInputElement;
    input.value = '1400';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-submit"]').click();
    fixture.detectChanges();

    expect(mockBudgetItemApi.recordItemSpent).toHaveBeenCalledWith(
      mockBudgetId,
      mockCategoryId,
      'item-1',
      { amount: 1400, currency: 'ZAR' },
    );
    expect(updatedSpy).toHaveBeenCalledWith(updatedItem);
  });

  it('cancel returns to idle mode', () => {
    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-realize"]').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="input-amount"]')).toBeTruthy();

    fixture.nativeElement.querySelector('[data-testid="btn-cancel"]').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="input-amount"]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-testid="btn-realize"]')).toBeTruthy();
  });

  it('does not call API when form is invalid (empty amount)', () => {
    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-realize"]').click();
    fixture.detectChanges();

    // Leave amount empty - form is invalid
    const component = fixture.componentInstance;
    expect(component.amountForm.invalid).toBe(true);

    fixture.nativeElement.querySelector('[data-testid="btn-submit"]').click();
    fixture.detectChanges();

    expect(mockBudgetItemApi.realizeItem).not.toHaveBeenCalled();
    expect(component.amountForm.controls.amount.touched).toBe(true);
  });

  it('shows error message on API failure', () => {
    const apiError = networkError(500, 'Server error');
    mockBudgetItemApi.realizeItem.mockReturnValue(of(failure(apiError)));

    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-realize"]').click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-amount"]',
    ) as HTMLInputElement;
    input.value = '1600';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-submit"]').click();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('[data-testid="error-message"]');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Server error');
  });

  it('save button disabled while saving', () => {
    const subject = new Subject<Result<BudgetItemDto, ApiError>>();
    mockBudgetItemApi.realizeItem.mockReturnValue(subject.asObservable());

    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-realize"]').click();
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-amount"]',
    ) as HTMLInputElement;
    input.value = '1600';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-submit"]').click();
    fixture.detectChanges();

    const submitBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-submit"]',
    ) as HTMLButtonElement;
    expect(submitBtn.disabled).toBe(true);
    expect(submitBtn.textContent).toContain('Saving...');

    subject.next(success(mockBudgetItemDto({ realizedAmount: 1600 })));
    fixture.detectChanges();

    // After completion, should return to idle
    expect(fixture.nativeElement.querySelector('[data-testid="btn-realize"]')).toBeTruthy();
  });
});
