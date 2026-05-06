import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetItemApiService, BudgetItemDto } from 'data-access-menlo-api';
import { ApiError, Result, failure, networkError, success } from 'shared-util';
import { BudgetItemsWorkspaceComponent } from './budget-items-workspace.component';

const mockBudgetId = 'budget-1';
const mockCategoryId = 'cat-1';

function mockItem(overrides: Partial<BudgetItemDto> = {}): BudgetItemDto {
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
    payerSplit: [],
    attributionSplit: [],
    adjustmentRuleId: null,
    isManualOverride: false,
    ...overrides,
  };
}

describe('BudgetItemsWorkspaceComponent', () => {
  let mockApi: {
    listItems: ReturnType<typeof vi.fn>;
    updateItem: ReturnType<typeof vi.fn>;
    deleteItem: ReturnType<typeof vi.fn>;
    realizeItem: ReturnType<typeof vi.fn>;
    recordItemSpent: ReturnType<typeof vi.fn>;
    fillForward: ReturnType<typeof vi.fn>;
    bulkCreateItems: ReturnType<typeof vi.fn>;
    createItem: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockApi = {
      listItems: vi.fn().mockReturnValue(of(success([]))),
      updateItem: vi.fn(),
      deleteItem: vi.fn(),
      realizeItem: vi.fn(),
      recordItemSpent: vi.fn(),
      fillForward: vi.fn(),
      bulkCreateItems: vi.fn(),
      createItem: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [BudgetItemsWorkspaceComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetItemApiService, useValue: mockApi },
      ],
    }).compileComponents();
  });

  function createComponent(budgetId = mockBudgetId, categoryId = mockCategoryId, categoryName = '') {
    const fixture = TestBed.createComponent(BudgetItemsWorkspaceComponent);
    fixture.componentRef.setInput('budgetId', budgetId);
    fixture.componentRef.setInput('categoryId', categoryId);
    if (categoryName) {
      fixture.componentRef.setInput('categoryName', categoryName);
    }
    fixture.detectChanges();
    return fixture;
  }

  // ── Loading state ──────────────────────────────────────────────────────────

  it('shows loading indicator while fetching items', () => {
    const subject = new Subject<Result<BudgetItemDto[], ApiError>>();
    mockApi.listItems.mockReturnValue(subject.asObservable());

    const fixture = createComponent();

    expect(fixture.nativeElement.querySelector('[data-testid="state-loading"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="items-list"]')).toBeNull();

    subject.next(success([]));
    fixture.detectChanges();
  });

  // ── Error state ────────────────────────────────────────────────────────────

  it('shows error message when listItems fails', () => {
    const apiError = networkError(500, 'Server error');
    mockApi.listItems.mockReturnValue(of(failure(apiError)));

    const fixture = createComponent();

    const el = fixture.nativeElement.querySelector('[data-testid="state-error"]');
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('Server error');
  });

  // ── Empty state ────────────────────────────────────────────────────────────

  it('shows empty message when no items returned', () => {
    mockApi.listItems.mockReturnValue(of(success([])));

    const fixture = createComponent();

    expect(fixture.nativeElement.querySelector('[data-testid="state-empty"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="items-list"]')).toBeNull();
  });

  // ── Populated state ────────────────────────────────────────────────────────

  it('renders item rows when items are returned', () => {
    const items = [mockItem({ id: 'a', month: 1 }), mockItem({ id: 'b', month: 2 })];
    mockApi.listItems.mockReturnValue(of(success(items)));

    const fixture = createComponent();

    const list = fixture.nativeElement.querySelector('[data-testid="items-list"]');
    expect(list).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="item-row-a"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('[data-testid="item-row-b"]')).toBeTruthy();
  });

  it('renders category title when categoryName input is provided', () => {
    const fixture = createComponent(mockBudgetId, mockCategoryId, 'Groceries');

    const title = fixture.nativeElement.querySelector('[data-testid="category-title"]');
    expect(title).toBeTruthy();
    expect(title.textContent).toContain('Groceries');
  });

  it('does not render category title when categoryName is empty', () => {
    const fixture = createComponent();

    expect(fixture.nativeElement.querySelector('[data-testid="category-title"]')).toBeNull();
  });

  // ── Edit panel ─────────────────────────────────────────────────────────────

  it('shows edit panel when edit button is clicked', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    expect(fixture.nativeElement.querySelector(`[data-testid="edit-panel-${item.id}"]`)).toBeNull();

    fixture.nativeElement.querySelector(`[data-testid="btn-edit-${item.id}"]`).click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="edit-panel-${item.id}"]`),
    ).toBeTruthy();
  });

  it('closes edit panel when edit button is clicked again (toggle)', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    const editBtn = fixture.nativeElement.querySelector(`[data-testid="btn-edit-${item.id}"]`);
    editBtn.click();
    fixture.detectChanges();
    editBtn.click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="edit-panel-${item.id}"]`),
    ).toBeNull();
  });

  it('hides the edit panel when edit is cancelled', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    fixture.nativeElement.querySelector(`[data-testid="btn-edit-${item.id}"]`).click();
    fixture.detectChanges();

    fixture.componentInstance.cancelEdit();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="edit-panel-${item.id}"]`),
    ).toBeNull();
  });

  // ── Fill-forward panel ─────────────────────────────────────────────────────

  it('shows fill-forward panel when fill-forward button is clicked', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="fill-forward-panel-${item.id}"]`),
    ).toBeNull();

    fixture.nativeElement.querySelector(`[data-testid="btn-fill-forward-${item.id}"]`).click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="fill-forward-panel-${item.id}"]`),
    ).toBeTruthy();
  });

  it('opening fill-forward closes any open edit panel', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    fixture.nativeElement.querySelector(`[data-testid="btn-edit-${item.id}"]`).click();
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector(`[data-testid="edit-panel-${item.id}"]`),
    ).toBeTruthy();

    fixture.nativeElement.querySelector(`[data-testid="btn-fill-forward-${item.id}"]`).click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="edit-panel-${item.id}"]`),
    ).toBeNull();
    expect(
      fixture.nativeElement.querySelector(`[data-testid="fill-forward-panel-${item.id}"]`),
    ).toBeTruthy();
  });

  it('closes fill-forward panel when fill-forward button is clicked again (toggle)', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    const fillForwardBtn = fixture.nativeElement.querySelector(
      `[data-testid="btn-fill-forward-${item.id}"]`,
    );

    fillForwardBtn.click();
    fixture.detectChanges();
    fillForwardBtn.click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="fill-forward-panel-${item.id}"]`),
    ).toBeNull();
  });

  it('hides the fill-forward panel when fill-forward is cancelled', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    fixture.nativeElement.querySelector(`[data-testid="btn-fill-forward-${item.id}"]`).click();
    fixture.detectChanges();

    fixture.componentInstance.cancelFillForward();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector(`[data-testid="fill-forward-panel-${item.id}"]`),
    ).toBeNull();
  });

  // ── Bulk-create panel ──────────────────────────────────────────────────────

  it('shows bulk-create panel when "Add Line Items" is clicked', () => {
    const fixture = createComponent();

    expect(fixture.nativeElement.querySelector('[data-testid="bulk-create-panel"]')).toBeNull();

    fixture.nativeElement.querySelector('[data-testid="btn-open-bulk-create"]').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="bulk-create-panel"]')).toBeTruthy();
  });

  it('hides bulk-create panel when bulk create is cancelled', () => {
    const fixture = createComponent();

    fixture.nativeElement.querySelector('[data-testid="btn-open-bulk-create"]').click();
    fixture.detectChanges();

    fixture.componentInstance.closeBulkCreate();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="bulk-create-panel"]')).toBeNull();
  });

  // ── Reload on child actions ────────────────────────────────────────────────

  it('reloads items after a delete', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));
    mockApi.deleteItem.mockReturnValue(of(success(undefined)));

    const fixture = createComponent();

    // Trigger delete confirmation flow
    fixture.nativeElement.querySelector('[data-testid="btn-delete"]').click();
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[data-testid="btn-confirm-yes"]').click();
    fixture.detectChanges();

    // listItems should have been called twice: initial load + after delete
    expect(mockApi.listItems).toHaveBeenCalledTimes(2);
  });

  it('updates item in place after a lifecycle action', () => {
    const item = mockItem({ plannedAmount: 500 });
    const updated = { ...item, realizedAmount: 450, realizedCurrency: 'ZAR' };
    mockApi.listItems.mockReturnValue(of(success([item])));
    mockApi.realizeItem.mockReturnValue(of(success(updated)));

    const fixture = createComponent();

    // Start realize via the lifecycle sub-component
    fixture.nativeElement.querySelector('[data-testid="btn-realize"]').click();
    fixture.detectChanges();

    const amountInput = fixture.nativeElement.querySelector(
      '[data-testid="input-amount"]',
    ) as HTMLInputElement;
    amountInput.value = '450';
    amountInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('[data-testid="btn-submit"]').click();
    fixture.detectChanges();

    // listItems should NOT have been called a second time (in-place update)
    expect(mockApi.listItems).toHaveBeenCalledTimes(1);

    // The updated amount should be visible
    const realizedEl = fixture.nativeElement.querySelector('[data-testid="realized-amount"]');
    expect(realizedEl).toBeTruthy();
    expect(realizedEl.textContent).toContain('450');
  });

  it('updates only the matching item after a lifecycle action', () => {
    const first = mockItem({ id: 'item-1', plannedAmount: 500 });
    const second = mockItem({ id: 'item-2', month: 4, plannedAmount: 900 });
    const updated = { ...first, realizedAmount: 450, realizedCurrency: 'ZAR' };
    mockApi.listItems.mockReturnValue(of(success([first, second])));

    const fixture = createComponent();

    fixture.componentInstance.onLifecycleUpdated(updated);
    fixture.detectChanges();

    expect(fixture.componentInstance.items()).toEqual([updated, second]);
  });

  it('reloads items and hides edit panel after a successful save', () => {
    const item = mockItem();
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    fixture.nativeElement.querySelector(`[data-testid="btn-edit-${item.id}"]`).click();
    fixture.detectChanges();

    fixture.componentInstance.onItemSaved(item);
    fixture.detectChanges();

    expect(mockApi.listItems).toHaveBeenCalledTimes(2);
    expect(
      fixture.nativeElement.querySelector(`[data-testid="edit-panel-${item.id}"]`),
    ).toBeNull();
  });

  it('reloads items and hides panel after bulk-create success', () => {
    const createdItems = [mockItem({ id: 'x1', month: 1 })];
    mockApi.listItems.mockReturnValue(of(success([])));

    const fixture = createComponent();

    fixture.nativeElement.querySelector('[data-testid="btn-open-bulk-create"]').click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="bulk-create-panel"]')).toBeTruthy();

    // Simulate a successful bulk-create event from the child component
    fixture.componentInstance.onBulkCreateSaved(createdItems);
    fixture.detectChanges();

    expect(mockApi.listItems).toHaveBeenCalledTimes(2);
    expect(fixture.nativeElement.querySelector('[data-testid="bulk-create-panel"]')).toBeNull();
  });

  it('reloads items and hides fill-forward panel after a successful fill-forward', () => {
    const item = mockItem();
    const createdItems = [mockItem({ id: 'x1', month: 4 })];
    mockApi.listItems.mockReturnValue(of(success([item])));

    const fixture = createComponent();

    fixture.nativeElement.querySelector(`[data-testid="btn-fill-forward-${item.id}"]`).click();
    fixture.detectChanges();

    fixture.componentInstance.onFillForwardDone(createdItems);
    fixture.detectChanges();

    expect(mockApi.listItems).toHaveBeenCalledTimes(2);
    expect(
      fixture.nativeElement.querySelector(`[data-testid="fill-forward-panel-${item.id}"]`),
    ).toBeNull();
  });

  // ── Single-create panel ────────────────────────────────────────────────────

  it('shows single-create panel when "Add Item" is clicked', () => {
    const fixture = createComponent();

    expect(
      fixture.nativeElement.querySelector('[data-testid="single-create-panel"]'),
    ).toBeNull();

    fixture.nativeElement.querySelector('[data-testid="btn-open-single-create"]').click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="single-create-panel"]'),
    ).toBeTruthy();
  });

  it('hides single-create panel when single create is cancelled', () => {
    const fixture = createComponent();

    fixture.nativeElement.querySelector('[data-testid="btn-open-single-create"]').click();
    fixture.detectChanges();

    fixture.componentInstance.closeSingleCreate();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="single-create-panel"]'),
    ).toBeNull();
  });

  it('reloads items and hides single-create panel after successful create', () => {
    const newItem = mockItem({ id: 'new-1' });
    mockApi.listItems.mockReturnValue(of(success([])));

    const fixture = createComponent();

    fixture.nativeElement.querySelector('[data-testid="btn-open-single-create"]').click();
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="single-create-panel"]'),
    ).toBeTruthy();

    fixture.componentInstance.onSingleCreateSaved(newItem);
    fixture.detectChanges();

    expect(mockApi.listItems).toHaveBeenCalledTimes(2);
    expect(
      fixture.nativeElement.querySelector('[data-testid="single-create-panel"]'),
    ).toBeNull();
  });
});
