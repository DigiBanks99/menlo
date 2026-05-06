import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetItemApiService, BudgetItemDto } from 'data-access-menlo-api';
import { ApiError, Result, failure, problemError, success } from 'shared-util';
import { BudgetItemFormComponent } from './budget-item-form.component';

const mockBudgetId = 'budget-1';
const mockCategoryId = 'cat-1';

function mockBudgetItemDto(overrides: Partial<BudgetItemDto> = {}): BudgetItemDto {
  return {
    id: 'item-1',
    budgetId: mockBudgetId,
    categoryId: mockCategoryId,
    month: 1,
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
    ...overrides,
  };
}

describe('BudgetItemFormComponent', () => {
  let mockBudgetItemApi: {
    updateItem: ReturnType<typeof vi.fn>;
    createItem: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockBudgetItemApi = {
      updateItem: vi.fn(),
      createItem: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [BudgetItemFormComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetItemApiService, useValue: mockBudgetItemApi },
      ],
    }).compileComponents();
  });

  describe('edit mode', () => {
    it('renders form with item data when item is provided', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const form = fixture.nativeElement.querySelector('[data-testid="budget-item-form"]');
      expect(form).toBeTruthy();

      const plannedInput = fixture.nativeElement.querySelector(
        '[data-testid="input-plannedAmount"]',
      ) as HTMLInputElement;
      expect(plannedInput.value).toBe('5000');
    });

    it('populates payer split from existing item', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      // Verify the FormArray has the correct number of controls
      const payerArray = fixture.componentInstance.form.controls.payerSplit;
      expect(payerArray.length).toBe(2);
      expect(payerArray.at(0).controls.userId.value).toBe('user-1');
      expect(payerArray.at(0).controls.percent.value).toBe(60);
    });
  });

  describe('payer split validation', () => {
    it('validates payer split sum equals 100', () => {
      const existing = mockBudgetItemDto({
        payerSplit: [{ userId: 'user-1', percent: 50 }],
      });

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const payerArray = fixture.componentInstance.form.controls.payerSplit;
      expect(payerArray.errors).toBeTruthy();
      expect(payerArray.errors!['splitSum']).toEqual({ actual: 50, required: 100 });
    });

    it('passes validation when payer split sums to 100', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const payerArray = fixture.componentInstance.form.controls.payerSplit;
      expect(payerArray.errors).toBeNull();
    });
  });

  describe('attribution split validation', () => {
    it('validates attribution split sum equals 100', () => {
      const existing = mockBudgetItemDto({
        attributionSplit: [{ attribution: 'Main', percent: 40 }],
      });

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const attrArray = fixture.componentInstance.form.controls.attributionSplit;
      expect(attrArray.errors).toBeTruthy();
      expect(attrArray.errors!['splitSum']).toEqual({ actual: 40, required: 100 });
    });

    it('passes validation when attribution split sums to 100', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const attrArray = fixture.componentInstance.form.controls.attributionSplit;
      expect(attrArray.errors).toBeNull();
    });
  });

  describe('submit', () => {
    it('submit is disabled when splits do not sum to 100', () => {
      const existing = mockBudgetItemDto({
        payerSplit: [{ userId: 'user-1', percent: 50 }],
      });

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      // Form is invalid due to split sum
      expect(fixture.componentInstance.form.invalid).toBe(true);

      // Attempting submit should not call API
      fixture.componentInstance.onSubmit();
      expect(mockBudgetItemApi.updateItem).not.toHaveBeenCalled();
    });

    it('calls updateItem API with correct partial request', () => {
      const existing = mockBudgetItemDto();
      const updatedDto = mockBudgetItemDto({ plannedAmount: 6000 });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ plannedAmount: 6000 });
      fixture.componentInstance.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        expect.objectContaining({
          plannedAmount: 6000,
          plannedCurrency: 'ZAR',
        }),
      );
    });

    it('emits saved event on successful update', () => {
      const existing = mockBudgetItemDto();
      const updatedDto = mockBudgetItemDto({ plannedAmount: 6000 });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const savedSpy = vi.fn();
      fixture.componentInstance.saved.subscribe(savedSpy);

      fixture.componentInstance.form.patchValue({ plannedAmount: 6000 });
      fixture.componentInstance.onSubmit();

      expect(savedSpy).toHaveBeenCalledWith(updatedDto);
    });

    it('only sends changed fields in update request', () => {
      const existing = mockBudgetItemDto();
      const updatedDto = mockBudgetItemDto();
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      // Submit without changes
      fixture.componentInstance.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        {},
      );
    });
  });

  describe('cancel', () => {
    it('emits cancelled output', () => {
      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.detectChanges();

      const cancelledSpy = vi.fn();
      fixture.componentInstance.cancelled.subscribe(cancelledSpy);

      fixture.componentInstance.onCancel();

      expect(cancelledSpy).toHaveBeenCalled();
    });
  });

  describe('error handling', () => {
    it('displays API errors on failure', () => {
      const existing = mockBudgetItemDto();
      mockBudgetItemApi.updateItem.mockReturnValue(
        of(failure(problemError({ title: 'Server error', status: 500 }, 500))),
      );

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ plannedAmount: 9999 });
      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('[data-testid="form-error"]');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent).toContain('Server error');
    });

    it('maps validation errors to form fields', () => {
      const existing = mockBudgetItemDto();
      mockBudgetItemApi.updateItem.mockReturnValue(
        of(
          failure(
            problemError(
              {
                title: 'Validation failed',
                status: 422,
                errors: { plannedAmount: ['Amount must be positive'] },
              },
              422,
            ),
          ),
        ),
      );

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ plannedAmount: -100 });
      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const amountControl = fixture.componentInstance.form.get('plannedAmount');
      expect(amountControl?.errors).toBeTruthy();
    });

    it('shows loading state during save', () => {
      const existing = mockBudgetItemDto();
      const subject = new Subject<Result<BudgetItemDto, ApiError>>();
      mockBudgetItemApi.updateItem.mockReturnValue(subject.asObservable());

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ plannedAmount: 7000 });
      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector(
        '[data-testid="btn-save"]',
      ) as HTMLButtonElement;
      expect(btn.disabled).toBe(true);
      expect(btn.textContent?.trim()).toContain('Saving...');
    });
  });

  describe('split management', () => {
    it('removePayerSplit removes the split at given index', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      expect(component.form.controls.payerSplit.length).toBe(2);

      component.removePayerSplit(0);
      expect(component.form.controls.payerSplit.length).toBe(1);
      expect(component.form.controls.payerSplit.at(0).controls.userId.value).toBe('user-2');
    });

    it('removeAttributionSplit removes the split at given index', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      expect(component.form.controls.attributionSplit.length).toBe(2);

      component.removeAttributionSplit(0);
      expect(component.form.controls.attributionSplit.length).toBe(1);
      expect(component.form.controls.attributionSplit.at(0).controls.attribution.value).toBe(
        'Rental',
      );
    });

    it('valueChanges subscription notifies split changes when item exists', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      const initialTotal = component.payerSplitTotal();

      // Change a payer percent value — should trigger valueChanges subscription
      component.form.controls.payerSplit.at(0).controls.percent.setValue(80);
      fixture.detectChanges();

      expect(component.payerSplitTotal()).toBe(120); // 80 + 40
      expect(component.payerSplitTotal()).not.toBe(initialTotal);
    });
  });

  describe('buildUpdateRequest branches', () => {
    it('includes realized changes in update request', () => {
      const existing = mockBudgetItemDto({ realizedAmount: null, realizedCurrency: null });
      const updatedDto = mockBudgetItemDto({ realizedAmount: 4500, realizedCurrency: 'ZAR' });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ realizedAmount: 4500 });
      fixture.componentInstance.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        expect.objectContaining({
          realizedAmount: 4500,
          realizedCurrency: 'ZAR',
        }),
      );
    });

    it('includes spent changes in update request', () => {
      const existing = mockBudgetItemDto({ spentAmount: null, spentCurrency: null });
      const updatedDto = mockBudgetItemDto({ spentAmount: 3200, spentCurrency: 'ZAR' });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ spentAmount: 3200 });
      fixture.componentInstance.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        expect.objectContaining({
          spentAmount: 3200,
          spentCurrency: 'ZAR',
        }),
      );
    });

    it('clears realized when set to null', () => {
      const existing = mockBudgetItemDto({ realizedAmount: 4000, realizedCurrency: 'ZAR' });
      const updatedDto = mockBudgetItemDto({ realizedAmount: null, realizedCurrency: null });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ realizedAmount: null });
      fixture.componentInstance.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        expect.objectContaining({
          realizedAmount: undefined,
          realizedCurrency: undefined,
        }),
      );
    });

    it('includes payer split changes in update request', () => {
      const existing = mockBudgetItemDto();
      const updatedDto = mockBudgetItemDto({
        payerSplit: [{ userId: 'user-1', percent: 100 }],
      });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      // Remove second payer and set first to 100%
      component.removePayerSplit(1);
      component.form.controls.payerSplit.at(0).controls.percent.setValue(100);
      component.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        expect.objectContaining({
          payerSplit: [{ userId: 'user-1', percent: 100 }],
        }),
      );
    });

    it('includes attribution split changes in update request', () => {
      const existing = mockBudgetItemDto();
      const updatedDto = mockBudgetItemDto({
        attributionSplit: [{ attribution: 'Main', percent: 100 }],
      });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      // Remove second attribution and set first to 100%
      component.removeAttributionSplit(1);
      component.form.controls.attributionSplit.at(0).controls.percent.setValue(100);
      component.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        expect.objectContaining({
          attributionSplit: [{ attribution: 'Main', percent: 100 }],
        }),
      );
    });

    it('clears spent when set to null', () => {
      const existing = mockBudgetItemDto({ spentAmount: 3000, spentCurrency: 'ZAR' });
      const updatedDto = mockBudgetItemDto({ spentAmount: null, spentCurrency: null });
      mockBudgetItemApi.updateItem.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ spentAmount: null });
      fixture.componentInstance.onSubmit();

      expect(mockBudgetItemApi.updateItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        'item-1',
        expect.objectContaining({
          spentAmount: undefined,
          spentCurrency: undefined,
        }),
      );
    });

    it('calls createItem when no item provided and form is valid', () => {
      const createdDto = mockBudgetItemDto({ id: 'new-1' });
      mockBudgetItemApi.createItem.mockReturnValue(of(success(createdDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      component.addPayerSplit('user-1', 100);
      component.addAttributionSplit('Main', 100);
      component.form.patchValue({ plannedAmount: 5000, month: 3, budgetFlow: 'Expense' });

      component.onSubmit();

      expect(mockBudgetItemApi.createItem).toHaveBeenCalledWith(
        mockBudgetId,
        mockCategoryId,
        expect.objectContaining({
          month: 3,
          budgetFlow: 'Expense',
          plannedAmount: 5000,
          plannedCurrency: 'ZAR',
        }),
      );
      expect(mockBudgetItemApi.updateItem).not.toHaveBeenCalled();
    });

    it('splitSumValidator handles null percent values in form', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      // Set a percent to null to exercise the ?? 0 branch in splitSumValidator
      component.form.controls.payerSplit.at(0).controls.percent.setValue(
        null as unknown as number,
        { emitEvent: false },
      );
      component.form.controls.payerSplit.updateValueAndValidity({ emitEvent: false });

      expect(component.form.controls.payerSplit.errors).toBeTruthy();
      expect(component.form.controls.payerSplit.errors!['splitSum'].actual).toBe(40);
    });

    it('payerSplitTotal computed handles null percent values', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      // Set percent to null to trigger ?? 0
      component.form.controls.payerSplit.at(0).controls.percent.setValue(
        null as unknown as number,
      );

      expect(component.payerSplitTotal()).toBe(40); // 0 + 40
    });

    it('attributionSplitTotal computed handles null percent values', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      // Set percent to null to trigger ?? 0
      component.form.controls.attributionSplit.at(0).controls.percent.setValue(
        null as unknown as number,
      );

      expect(component.attributionSplitTotal()).toBe(30); // 0 + 30
    });
  });

  describe('create mode', () => {
    it('shows month and budgetFlow fields when no item is provided', () => {
      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="input-month"]')).toBeTruthy();
      expect(
        fixture.nativeElement.querySelector('[data-testid="select-budgetFlow"]'),
      ).toBeTruthy();
    });

    it('does not show month and budgetFlow fields in edit mode', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="input-month"]')).toBeNull();
      expect(
        fixture.nativeElement.querySelector('[data-testid="select-budgetFlow"]'),
      ).toBeNull();
    });

    it('shows Create button label in create mode', () => {
      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector('[data-testid="btn-save"]') as HTMLButtonElement;
      expect(btn.textContent?.trim()).toBe('Create');
    });

    it('shows Update button label in edit mode', () => {
      const existing = mockBudgetItemDto();

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.componentRef.setInput('item', existing);
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector('[data-testid="btn-save"]') as HTMLButtonElement;
      expect(btn.textContent?.trim()).toBe('Update');
    });

    it('emits saved with created item on successful create', () => {
      const createdDto = mockBudgetItemDto({ id: 'new-1', month: 5, budgetFlow: 'Income' });
      mockBudgetItemApi.createItem.mockReturnValue(of(success(createdDto)));

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.detectChanges();

      const savedSpy = vi.fn();
      fixture.componentInstance.saved.subscribe(savedSpy);

      fixture.componentInstance.addPayerSplit('user-1', 100);
      fixture.componentInstance.addAttributionSplit('Main', 100);
      fixture.componentInstance.form.patchValue({ plannedAmount: 3000, month: 5, budgetFlow: 'Income' });
      fixture.componentInstance.onSubmit();

      expect(savedSpy).toHaveBeenCalledWith(createdDto);
    });

    it('shows error banner on create failure', () => {
      mockBudgetItemApi.createItem.mockReturnValue(
        of(failure(problemError({ title: 'Create failed', status: 500 }, 500))),
      );

      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.detectChanges();

      fixture.componentInstance.addPayerSplit('user-1', 100);
      fixture.componentInstance.addAttributionSplit('Main', 100);
      fixture.componentInstance.form.patchValue({ plannedAmount: 2000, month: 1, budgetFlow: 'Expense' });
      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('[data-testid="form-error"]');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent).toContain('Create failed');
    });

    it('does not submit when budgetFlow is not selected', () => {
      const fixture = TestBed.createComponent(BudgetItemFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('categoryId', mockCategoryId);
      fixture.detectChanges();

      const component = fixture.componentInstance;
      component.addPayerSplit('user-1', 100);
      component.addAttributionSplit('Main', 100);
      component.form.patchValue({ plannedAmount: 5000, month: 3 });
      // budgetFlow left as '' (invalid)

      component.onSubmit();

      expect(mockBudgetItemApi.createItem).not.toHaveBeenCalled();
    });
  });
});
