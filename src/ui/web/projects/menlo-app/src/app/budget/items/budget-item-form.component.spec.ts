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
  };

  beforeEach(async () => {
    mockBudgetItemApi = {
      updateItem: vi.fn(),
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
});
