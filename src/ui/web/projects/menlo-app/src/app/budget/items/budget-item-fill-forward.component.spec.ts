import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  BudgetItemApiService,
  BudgetItemDto,
  FillForwardBudgetItemRequest,
} from 'data-access-menlo-api';
import { ApiError, Result, failure, networkError, success } from 'shared-util';
import { BudgetItemFillForwardComponent } from './budget-item-fill-forward.component';

const mockBudgetId = 'budget-1';
const mockCategoryId = 'cat-1';

function mockBudgetItemDto(overrides: Partial<BudgetItemDto> = {}): BudgetItemDto {
  return {
    id: 'item-1',
    budgetId: mockBudgetId,
    categoryId: mockCategoryId,
    month: 3,
    budgetFlow: 'Expense',
    plannedAmount: 2000,
    plannedCurrency: 'ZAR',
    realizedAmount: null,
    realizedCurrency: null,
    spentAmount: null,
    spentCurrency: null,
    payerSplit: [{ userId: 'user-1', percent: 100 }],
    attributionSplit: [{ attribution: 'Main', percent: 100 }],
    adjustmentRuleId: null,
    isManualOverride: false,
    ...overrides,
  };
}

function formatAmount(value: number): string {
  return new Intl.NumberFormat('en-ZA', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

describe('BudgetItemFillForwardComponent', () => {
  let mockBudgetItemApi: {
    fillForward: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockBudgetItemApi = {
      fillForward: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [BudgetItemFillForwardComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetItemApiService, useValue: mockBudgetItemApi },
      ],
    }).compileComponents();
  });

  function createComponent(item?: BudgetItemDto) {
    const fixture = TestBed.createComponent(BudgetItemFillForwardComponent);
    fixture.componentRef.setInput('budgetId', mockBudgetId);
    fixture.componentRef.setInput('categoryId', mockCategoryId);
    fixture.componentRef.setInput('item', item ?? mockBudgetItemDto());
    fixture.detectChanges();
    return fixture;
  }

  it('shows item amount and month info', () => {
    const fixture = createComponent();
    const info = fixture.nativeElement.querySelector('[data-testid="fill-forward-info"]');
    expect(info).toBeTruthy();
    expect(info.textContent).toContain('ZAR');
    expect(info.textContent).toContain('2000');
    expect(info.textContent).toContain('3');
  });

  it('submit calls fillForward API', () => {
    const filledItems: BudgetItemDto[] = [
      mockBudgetItemDto({ month: 3 }),
      mockBudgetItemDto({ month: 4 }),
    ];
    mockBudgetItemApi.fillForward.mockReturnValue(of(success(filledItems)));

    const fixture = createComponent();
    const amountInput = fixture.nativeElement.querySelector(
      '[data-testid="input-amount"]',
    ) as HTMLInputElement;
    amountInput.value = '2500';
    amountInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const submitBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-submit"]',
    ) as HTMLButtonElement;
    submitBtn.click();
    fixture.detectChanges();

    expect(mockBudgetItemApi.fillForward).toHaveBeenCalledWith(mockBudgetId, mockCategoryId, {
      fromMonth: 3,
      budgetFlow: 'Expense',
      amount: 2500,
      currency: 'ZAR',
      payerSplit: [{ userId: 'user-1', percent: 100 }],
      attributionSplit: [{ attribution: 'Main', percent: 100 }],
    } satisfies FillForwardBudgetItemRequest);
  });

  it('on success, emits filled output', () => {
    const filledItems: BudgetItemDto[] = [
      mockBudgetItemDto({ month: 3 }),
      mockBudgetItemDto({ month: 4 }),
    ];
    mockBudgetItemApi.fillForward.mockReturnValue(of(success(filledItems)));

    const fixture = createComponent();
    const filledSpy = vi.fn();
    fixture.componentInstance.filled.subscribe(filledSpy);

    const submitBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-submit"]',
    ) as HTMLButtonElement;
    submitBtn.click();
    fixture.detectChanges();

    expect(filledSpy).toHaveBeenCalledWith(filledItems);
  });

  it('cancel emits cancelled', () => {
    const fixture = createComponent();
    const cancelledSpy = vi.fn();
    fixture.componentInstance.cancelled.subscribe(cancelledSpy);

    const cancelBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-cancel"]',
    ) as HTMLButtonElement;
    cancelBtn.click();
    fixture.detectChanges();

    expect(cancelledSpy).toHaveBeenCalled();
  });

  it('shows error on failure', () => {
    mockBudgetItemApi.fillForward.mockReturnValue(of(failure(networkError('Server error'))));

    const fixture = createComponent();
    const submitBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-submit"]',
    ) as HTMLButtonElement;
    submitBtn.click();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('[data-testid="error-message"]');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toBeTruthy();
  });

  it('does not call API when form is invalid (empty amount)', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    // Clear the amount to make form invalid
    component.form.controls.amount.setValue(null as unknown as number);
    fixture.detectChanges();

    expect(component.form.invalid).toBe(true);

    component.onSubmit();

    expect(mockBudgetItemApi.fillForward).not.toHaveBeenCalled();
    expect(component.form.controls.amount.touched).toBe(true);
  });

  it('amount can be edited before submitting', () => {
    const filledItems: BudgetItemDto[] = [mockBudgetItemDto({ month: 3 })];
    mockBudgetItemApi.fillForward.mockReturnValue(of(success(filledItems)));

    const fixture = createComponent();
    const amountInput = fixture.nativeElement.querySelector(
      '[data-testid="input-amount"]',
    ) as HTMLInputElement;

    // Default should be the item's planned amount
    expect(amountInput.value).toBe(formatAmount(2000));

    // Change it
    amountInput.value = '3500';
    amountInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const submitBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-submit"]',
    ) as HTMLButtonElement;
    submitBtn.click();
    fixture.detectChanges();

    expect(mockBudgetItemApi.fillForward).toHaveBeenCalledWith(
      mockBudgetId,
      mockCategoryId,
      expect.objectContaining({ amount: 3500 }),
    );
  });
});
