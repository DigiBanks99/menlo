import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { CategoryApiService, CategoryDto } from 'data-access-menlo-api';
import { ApiError, Result, failure, problemError, success } from 'shared-util';
import { CategoryFormComponent } from './category-form.component';

const mockBudgetId = 'budget-1';

function mockCategoryDto(overrides: Partial<CategoryDto> = {}): CategoryDto {
  return {
    id: 'cat-1',
    budgetId: mockBudgetId,
    name: 'Groceries',
    description: 'Monthly groceries',
    canonicalCategoryId: 'canon-1',
    budgetFlow: 'Expense',
    attribution: 'Main',
    incomeContributor: undefined,
    responsiblePayer: 'Dad',
    isDeleted: false,
    ...overrides,
  };
}

describe('CategoryFormComponent', () => {
  let mockCategoryApi: {
    createCategory: ReturnType<typeof vi.fn>;
    updateCategory: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockCategoryApi = {
      createCategory: vi.fn(),
      updateCategory: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [CategoryFormComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: CategoryApiService, useValue: mockCategoryApi },
      ],
    }).compileComponents();
  });

  describe('create mode', () => {
    it('renders form with empty fields', () => {
      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      const form = fixture.nativeElement.querySelector('[data-testid="category-form"]');
      expect(form).toBeTruthy();

      const nameInput = fixture.nativeElement.querySelector(
        '[data-testid="input-name"]',
      ) as HTMLInputElement;
      expect(nameInput.value).toBe('');
    });

    it('shows validation errors when submitting empty form', () => {
      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const nameError = fixture.nativeElement.querySelector('[data-testid="error-name"]');
      expect(nameError).toBeTruthy();

      const flowError = fixture.nativeElement.querySelector('[data-testid="error-budgetFlow"]');
      expect(flowError).toBeTruthy();
    });

    it('calls createCategory on valid submit', () => {
      const createdDto = mockCategoryDto();
      mockCategoryApi.createCategory.mockReturnValue(of(success(createdDto)));

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({
        name: 'Groceries',
        budgetFlow: 'Expense',
      });
      fixture.componentInstance.onSubmit();

      expect(mockCategoryApi.createCategory).toHaveBeenCalledWith(
        mockBudgetId,
        expect.objectContaining({
          name: 'Groceries',
          budgetFlow: 'Expense',
        }),
      );
    });

    it('emits saved event on successful create', () => {
      const createdDto = mockCategoryDto();
      mockCategoryApi.createCategory.mockReturnValue(of(success(createdDto)));

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      const savedSpy = vi.fn();
      fixture.componentInstance.saved.subscribe(savedSpy);

      fixture.componentInstance.form.patchValue({
        name: 'Groceries',
        budgetFlow: 'Expense',
      });
      fixture.componentInstance.onSubmit();

      expect(savedSpy).toHaveBeenCalledWith(createdDto);
    });

    it('includes parentId when provided', () => {
      const createdDto = mockCategoryDto();
      mockCategoryApi.createCategory.mockReturnValue(of(success(createdDto)));

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('parentId', 'parent-1');
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({
        name: 'Sub Category',
        budgetFlow: 'Income',
      });
      fixture.componentInstance.onSubmit();

      expect(mockCategoryApi.createCategory).toHaveBeenCalledWith(
        mockBudgetId,
        expect.objectContaining({
          parentId: 'parent-1',
        }),
      );
    });

    it('shows loading state during save', () => {
      const subject = new Subject<Result<CategoryDto, ApiError>>();
      mockCategoryApi.createCategory.mockReturnValue(subject.asObservable());

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({
        name: 'Test',
        budgetFlow: 'Both',
      });
      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector(
        '[data-testid="btn-save"]',
      ) as HTMLButtonElement;
      expect(btn.disabled).toBe(true);
      expect(btn.textContent?.trim()).toContain('Saving...');
    });

    it('shows form error on failure', () => {
      mockCategoryApi.createCategory.mockReturnValue(
        of(failure(problemError({ title: 'Server error', status: 500 }, 500))),
      );

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({
        name: 'Test',
        budgetFlow: 'Expense',
      });
      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('[data-testid="form-error"]');
      expect(errorEl).toBeTruthy();
    });

    it('maps 409 conflict to name field error', () => {
      mockCategoryApi.createCategory.mockReturnValue(
        of(
          failure(
            problemError(
              { title: 'Duplicate name', status: 409, detail: 'Category already exists' },
              409,
            ),
          ),
        ),
      );

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({
        name: 'Duplicate',
        budgetFlow: 'Expense',
      });
      fixture.componentInstance.onSubmit();
      fixture.detectChanges();

      const nameError = fixture.nativeElement.querySelector('[data-testid="error-name"]');
      expect(nameError).toBeTruthy();
    });
  });

  describe('edit mode', () => {
    it('populates form with existing category data', () => {
      const existing = mockCategoryDto();

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('category', existing);
      fixture.detectChanges();

      const nameInput = fixture.nativeElement.querySelector(
        '[data-testid="input-name"]',
      ) as HTMLInputElement;
      expect(nameInput.value).toBe('Groceries');
    });

    it('calls updateCategory on submit in edit mode', () => {
      const existing = mockCategoryDto();
      const updatedDto = mockCategoryDto({ name: 'Updated' });
      mockCategoryApi.updateCategory.mockReturnValue(of(success(updatedDto)));

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('category', existing);
      fixture.detectChanges();

      fixture.componentInstance.form.patchValue({ name: 'Updated' });
      fixture.componentInstance.onSubmit();

      expect(mockCategoryApi.updateCategory).toHaveBeenCalledWith(
        mockBudgetId,
        'cat-1',
        expect.objectContaining({ name: 'Updated' }),
      );
    });

    it('shows Update button text in edit mode', () => {
      const existing = mockCategoryDto();

      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.componentRef.setInput('category', existing);
      fixture.detectChanges();

      const btn = fixture.nativeElement.querySelector(
        '[data-testid="btn-save"]',
      ) as HTMLButtonElement;
      expect(btn.textContent?.trim()).toContain('Update');
    });
  });

  describe('cancel', () => {
    it('emits cancelled event', () => {
      const fixture = TestBed.createComponent(CategoryFormComponent);
      fixture.componentRef.setInput('budgetId', mockBudgetId);
      fixture.detectChanges();

      const cancelledSpy = vi.fn();
      fixture.componentInstance.cancelled.subscribe(cancelledSpy);

      fixture.componentInstance.onCancel();

      expect(cancelledSpy).toHaveBeenCalled();
    });
  });
});
