import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetItemApiService, BudgetItemDto } from 'data-access-menlo-api';
import { ApiError, Result, failure, problemError, success } from 'shared-util';
import { BudgetItemBulkCreateComponent } from './budget-item-bulk-create.component';

const mockBudgetId = 'budget-1';
const mockCategoryId = 'cat-1';

function mockBudgetItemDto(month: number): BudgetItemDto {
  return {
    id: `item-${month}`,
    budgetId: mockBudgetId,
    categoryId: mockCategoryId,
    month,
    budgetFlow: 'Expense',
    plannedAmount: 5000,
    plannedCurrency: 'ZAR',
    realizedAmount: null,
    realizedCurrency: null,
    spentAmount: null,
    spentCurrency: null,
    payerSplit: [
      { userId: 'user-1', percent: 60 },
      { userId: 'user-2', percent: 40 },
    ],
    attributionSplit: [
      { attribution: 'Main', percent: 70 },
      { attribution: 'Rental', percent: 30 },
    ],
    adjustmentRuleId: null,
    isManualOverride: false,
  };
}

function mockBulkResponse(): BudgetItemDto[] {
  return Array.from({ length: 12 }, (_, i) => mockBudgetItemDto(i + 1));
}

describe('BudgetItemBulkCreateComponent', () => {
  let mockBudgetItemApi: {
    bulkCreateItems: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockBudgetItemApi = {
      bulkCreateItems: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [BudgetItemBulkCreateComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetItemApiService, useValue: mockBudgetItemApi },
      ],
    }).compileComponents();
  });

  function createComponent() {
    const fixture = TestBed.createComponent(BudgetItemBulkCreateComponent);
    fixture.componentRef.setInput('budgetId', mockBudgetId);
    fixture.componentRef.setInput('categoryId', mockCategoryId);
    fixture.detectChanges();
    return fixture;
  }

  it('renders form with budget flow select and amount input', () => {
    const fixture = createComponent();

    const form = fixture.nativeElement.querySelector('[data-testid="bulk-create-form"]');
    expect(form).toBeTruthy();

    const budgetFlowSelect = fixture.nativeElement.querySelector(
      '[data-testid="select-budget-flow"]',
    );
    expect(budgetFlowSelect).toBeTruthy();

    const amountInput = fixture.nativeElement.querySelector('[data-testid="input-amount"]');
    expect(amountInput).toBeTruthy();
  });

  it('submit is disabled when payer split does not total 100', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.patchValue({ budgetFlow: 'Expense', amount: 5000 });
    component.addPayerSplit('user-1', 50);
    component.addAttributionSplit('Main', 100);
    fixture.detectChanges();

    expect(component.form.invalid).toBe(true);
    component.onSubmit();
    expect(mockBudgetItemApi.bulkCreateItems).not.toHaveBeenCalled();
  });

  it('submit is disabled when attribution split does not total 100', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.patchValue({ budgetFlow: 'Expense', amount: 5000 });
    component.addPayerSplit('user-1', 100);
    component.addAttributionSplit('Main', 40);
    fixture.detectChanges();

    expect(component.form.invalid).toBe(true);
    component.onSubmit();
    expect(mockBudgetItemApi.bulkCreateItems).not.toHaveBeenCalled();
  });

  it('can add and remove payer split rows', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.addPayerSplit('user-1', 60);
    component.addPayerSplit('user-2', 40);
    expect(component.form.controls.payerSplit.length).toBe(2);

    component.removePayerSplit(0);
    expect(component.form.controls.payerSplit.length).toBe(1);
    expect(component.form.controls.payerSplit.at(0).controls.userId.value).toBe('user-2');
  });

  it('can add and remove attribution split rows', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.addAttributionSplit('Main', 70);
    component.addAttributionSplit('Rental', 30);
    expect(component.form.controls.attributionSplit.length).toBe(2);

    component.removeAttributionSplit(1);
    expect(component.form.controls.attributionSplit.length).toBe(1);
    expect(component.form.controls.attributionSplit.at(0).controls.attribution.value).toBe('Main');
  });

  it('submit calls bulkCreateItems API with correct request', () => {
    const response = mockBulkResponse();
    mockBudgetItemApi.bulkCreateItems.mockReturnValue(of(success(response)));

    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.patchValue({ budgetFlow: 'Expense', amount: 5000 });
    component.addPayerSplit('user-1', 60);
    component.addPayerSplit('user-2', 40);
    component.addAttributionSplit('Main', 70);
    component.addAttributionSplit('Rental', 30);

    component.onSubmit();

    expect(mockBudgetItemApi.bulkCreateItems).toHaveBeenCalledWith(mockBudgetId, mockCategoryId, {
      budgetFlow: 'Expense',
      amount: 5000,
      currency: 'ZAR',
      payerSplit: [
        { userId: 'user-1', percent: 60 },
        { userId: 'user-2', percent: 40 },
      ],
      attributionSplit: [
        { attribution: 'Main', percent: 70 },
        { attribution: 'Rental', percent: 30 },
      ],
    });
  });

  it('on success emits saved with array of items', () => {
    const response = mockBulkResponse();
    mockBudgetItemApi.bulkCreateItems.mockReturnValue(of(success(response)));

    const fixture = createComponent();
    const component = fixture.componentInstance;

    const savedSpy = vi.fn();
    component.saved.subscribe(savedSpy);

    component.form.patchValue({ budgetFlow: 'Expense', amount: 5000 });
    component.addPayerSplit('user-1', 100);
    component.addAttributionSplit('Main', 100);

    component.onSubmit();

    expect(savedSpy).toHaveBeenCalledWith(response);
    expect(response).toHaveLength(12);
  });

  it('cancel emits cancelled', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    const cancelledSpy = vi.fn();
    component.cancelled.subscribe(cancelledSpy);

    component.onCancel();

    expect(cancelledSpy).toHaveBeenCalled();
  });

  it('shows error on API failure', () => {
    mockBudgetItemApi.bulkCreateItems.mockReturnValue(
      of(failure(problemError({ title: 'Server error', status: 500 }, 500))),
    );

    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.patchValue({ budgetFlow: 'Expense', amount: 5000 });
    component.addPayerSplit('user-1', 100);
    component.addAttributionSplit('Main', 100);

    component.onSubmit();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('[data-testid="form-error"]');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Server error');
  });

  it('shows loading state during save', () => {
    const subject = new Subject<Result<BudgetItemDto[], ApiError>>();
    mockBudgetItemApi.bulkCreateItems.mockReturnValue(subject.asObservable());

    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.patchValue({ budgetFlow: 'Income', amount: 3000 });
    component.addPayerSplit('user-1', 100);
    component.addAttributionSplit('Main', 100);

    component.onSubmit();
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector(
      '[data-testid="btn-submit"]',
    ) as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
    expect(btn.textContent?.trim()).toContain('Creating...');
  });

  it('handles validation errors by mapping to form fields', () => {
    mockBudgetItemApi.bulkCreateItems.mockReturnValue(
      of(
        failure(
          problemError(
            {
              title: 'Validation failed',
              status: 422,
              errors: { amount: ['Amount must be positive'] },
            },
            422,
          ),
        ),
      ),
    );

    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.patchValue({ budgetFlow: 'Expense', amount: 5000 });
    component.addPayerSplit('user-1', 100);
    component.addAttributionSplit('Main', 100);

    component.onSubmit();
    fixture.detectChanges();

    const amountControl = component.form.get('amount');
    expect(amountControl?.errors).toBeTruthy();
  });

  it('surfaces API-specific amount errors through the amount helper', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.controls.amount.markAsTouched();
    component.form.controls.amount.setErrors({ api: 'Amount must be positive' });

    expect(
      (component as unknown as { amountErrorMessage(): string | null }).amountErrorMessage(),
    ).toBe('Amount must be positive');
  });

  it('returns the default amount validation message for non-API amount errors', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.controls.amount.markAsTouched();
    component.form.controls.amount.setErrors({ min: true });

    expect(
      (component as unknown as { amountErrorMessage(): string | null }).amountErrorMessage(),
    ).toBe('Amount must be positive');
  });

  it('surfaces required and API errors through the budget-flow helper', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.form.controls.budgetFlow.markAsTouched();
    component.form.controls.budgetFlow.setErrors({ required: true });
    expect(
      (component as unknown as { budgetFlowErrorMessage(): string | null }).budgetFlowErrorMessage(),
    ).toBe('Required');

    component.form.controls.budgetFlow.setErrors({ api: 'Choose a budget flow' });
    expect(
      (component as unknown as { budgetFlowErrorMessage(): string | null }).budgetFlowErrorMessage(),
    ).toBe('Choose a budget flow');
  });

  it('splitSumValidator handles null percent values', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    // Add a payer split — the validator runs on the FormArray
    component.addPayerSplit('user-1', 0);

    // Set to null without emitting events (avoids change detection issues)
    const group = component.form.controls.payerSplit.at(0);
    group.controls.percent.setValue(null as unknown as number, { emitEvent: false });

    // Manually trigger validation
    component.form.controls.payerSplit.updateValueAndValidity({ emitEvent: false });

    // The validator should treat null as 0
    expect(component.form.controls.payerSplit.errors).toBeTruthy();
    expect(component.form.controls.payerSplit.errors!['splitSum']).toEqual({
      actual: 0,
      required: 100,
    });
  });

  it('payerSplitTotal computed handles null percent values', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.addPayerSplit('user-1', 50);
    component.addPayerSplit('user-2', 50);
    fixture.detectChanges();

    // Set one percent to null to trigger ?? 0 branch in computed
    component.form.controls.payerSplit.at(1).controls.percent.setValue(
      null as unknown as number,
    );
    fixture.detectChanges();

    expect(component.payerSplitTotal()).toBe(50);
  });

  it('attributionSplitTotal computed handles null percent values', () => {
    const fixture = createComponent();
    const component = fixture.componentInstance;

    component.addAttributionSplit('Main', 70);
    component.addAttributionSplit('Rental', 30);
    fixture.detectChanges();

    // Set one percent to null to trigger ?? 0 branch in computed
    component.form.controls.attributionSplit.at(1).controls.percent.setValue(
      null as unknown as number,
    );
    fixture.detectChanges();

    expect(component.attributionSplitTotal()).toBe(70);
  });
});
