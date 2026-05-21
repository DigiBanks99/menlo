import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetItemApiService } from 'data-access-menlo-api';
import { ApiError, Result, failure, networkError, success } from 'shared-util';
import { BudgetItemDeleteComponent } from './budget-item-delete.component';

const mockBudgetId = 'budget-1';
const mockCategoryId = 'cat-1';
const mockItemId = 'item-1';

describe('BudgetItemDeleteComponent', () => {
  let mockBudgetItemApi: {
    deleteItem: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockBudgetItemApi = {
      deleteItem: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [BudgetItemDeleteComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetItemApiService, useValue: mockBudgetItemApi },
      ],
    }).compileComponents();
  });

  function createComponent() {
    const fixture = TestBed.createComponent(BudgetItemDeleteComponent);
    fixture.componentRef.setInput('budgetId', mockBudgetId);
    fixture.componentRef.setInput('categoryId', mockCategoryId);
    fixture.componentRef.setInput('itemId', mockItemId);
    fixture.detectChanges();
    return fixture;
  }

  it('shows delete button in initial state', () => {
    const fixture = createComponent();
    const btn = fixture.nativeElement.querySelector('[data-testid="btn-delete"]');
    expect(btn).toBeTruthy();
    expect(btn.textContent).toContain('Delete');
  });

  it('clicking delete shows confirmation prompt', () => {
    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-delete"]').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="btn-confirm-yes"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="btn-confirm-no"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="btn-delete"]')).toBeNull();
  });

  it('clicking "No" returns to initial state', () => {
    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-delete"]').click();
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-confirm-no"]').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="btn-delete"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="btn-confirm-yes"]')).toBeNull();
  });

  it('clicking "Yes, delete" calls deleteItem API', () => {
    mockBudgetItemApi.deleteItem.mockReturnValue(of(success(undefined)));

    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-delete"]').click();
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-confirm-yes"]').click();
    fixture.detectChanges();

    expect(mockBudgetItemApi.deleteItem).toHaveBeenCalledWith(
      mockBudgetId,
      mockCategoryId,
      mockItemId,
    );
  });

  it('on success, emits deleted output', () => {
    mockBudgetItemApi.deleteItem.mockReturnValue(of(success(undefined)));

    const fixture = createComponent();
    const deletedSpy = vi.fn();
    fixture.componentInstance.deleted.subscribe(deletedSpy);

    fixture.nativeElement.querySelector('[data-testid="btn-delete"]').click();
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-confirm-yes"]').click();
    fixture.detectChanges();

    expect(deletedSpy).toHaveBeenCalled();
  });

  it('on failure, shows error message', () => {
    const apiError = networkError(500, 'Internal server error');
    mockBudgetItemApi.deleteItem.mockReturnValue(of(failure(apiError)));

    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-delete"]').click();
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-confirm-yes"]').click();
    fixture.detectChanges();

    const errorEl = fixture.nativeElement.querySelector('[data-testid="delete-error"]');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Internal server error');
  });

  it('buttons disabled while deleting', () => {
    const subject = new Subject<Result<void, ApiError>>();
    mockBudgetItemApi.deleteItem.mockReturnValue(subject.asObservable());

    const fixture = createComponent();
    fixture.nativeElement.querySelector('[data-testid="btn-delete"]').click();
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-confirm-yes"]').click();
    fixture.detectChanges();

    const yesBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-confirm-yes"]',
    ) as HTMLButtonElement;
    const noBtn = fixture.nativeElement.querySelector(
      '[data-testid="btn-confirm-no"]',
    ) as HTMLButtonElement;
    expect(yesBtn.disabled).toBe(true);
    expect(yesBtn.textContent).toContain('Deleting...');
    expect(noBtn.disabled).toBe(true);

    subject.next(success(undefined));
    fixture.detectChanges();
  });

  it('does not cancel the confirmation state while deletion is in progress', () => {
    const fixture = createComponent();
    fixture.componentInstance.askConfirmation();
    (
      fixture.componentInstance as unknown as {
        deleting: { set(value: boolean): void };
      }
    ).deleting.set(true);

    fixture.componentInstance.cancelDelete();

    expect(fixture.componentInstance.confirming()).toBe(true);
  });
});
