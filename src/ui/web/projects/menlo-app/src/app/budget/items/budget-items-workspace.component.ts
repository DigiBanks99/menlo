import { Component, effect, inject, input, signal } from '@angular/core';
import { BudgetItemApiService, BudgetItemDto } from 'data-access-menlo-api';
import { getErrorMessage, isSuccess } from 'shared-util';
import { BudgetItemBulkCreateComponent } from './budget-item-bulk-create.component';
import { BudgetItemDeleteComponent } from './budget-item-delete.component';
import { BudgetItemFillForwardComponent } from './budget-item-fill-forward.component';
import { BudgetItemFormComponent } from './budget-item-form.component';
import { BudgetItemLifecycleComponent } from './budget-item-lifecycle.component';

@Component({
  selector: 'app-budget-items-workspace',
  imports: [
    BudgetItemLifecycleComponent,
    BudgetItemFormComponent,
    BudgetItemDeleteComponent,
    BudgetItemFillForwardComponent,
    BudgetItemBulkCreateComponent,
  ],
  template: `
    <div class="workspace" data-testid="budget-items-workspace">
      <div class="workspace-header">
        @if (categoryName()) {
          <h3 class="category-title" data-testid="category-title">{{ categoryName() }}</h3>
        }
        <div class="header-actions">
          <button
            type="button"
            class="btn-add-item"
            (click)="openSingleCreate()"
            [disabled]="showSingleCreate()"
            data-testid="btn-open-single-create"
          >
            Add Item
          </button>
          <button
            type="button"
            class="btn-bulk-create"
            (click)="openBulkCreate()"
            [disabled]="showBulkCreate()"
            data-testid="btn-open-bulk-create"
          >
            Add Line Items
          </button>
        </div>
      </div>

      @if (showSingleCreate()) {
        <div class="single-create-panel" data-testid="single-create-panel">
          <app-budget-item-form
            [budgetId]="budgetId()"
            [categoryId]="categoryId()"
            (saved)="onSingleCreateSaved($event)"
            (cancelled)="closeSingleCreate()"
          />
        </div>
      }

      @if (showBulkCreate()) {
        <div class="bulk-create-panel" data-testid="bulk-create-panel">
          <app-budget-item-bulk-create
            [budgetId]="budgetId()"
            [categoryId]="categoryId()"
            (saved)="onBulkCreateSaved($event)"
            (cancelled)="closeBulkCreate()"
          />
        </div>
      }

      @if (loading()) {
        <div class="state-loading" data-testid="state-loading">Loading items…</div>
      } @else if (loadError()) {
        <div class="state-error" data-testid="state-error">{{ loadError() }}</div>
      } @else if (items().length === 0) {
        <div class="state-empty" data-testid="state-empty">
          No items for this category yet.
        </div>
      } @else {
        <ul class="items-list" data-testid="items-list">
          @for (item of items(); track item.id) {
            <li class="item-row" [attr.data-testid]="'item-row-' + item.id">
              <div class="item-header">
                <span class="item-month" data-testid="item-month">Month {{ item.month }}</span>
                <span class="item-flow" data-testid="item-flow">{{ item.budgetFlow }}</span>
                <div class="item-actions">
                  <button
                    type="button"
                    class="btn-edit"
                    (click)="toggleEdit(item.id)"
                    [attr.data-testid]="'btn-edit-' + item.id"
                  >
                    {{ editingItemId() === item.id ? 'Cancel Edit' : 'Edit' }}
                  </button>
                  <button
                    type="button"
                    class="btn-fill-forward"
                    (click)="toggleFillForward(item.id)"
                    [attr.data-testid]="'btn-fill-forward-' + item.id"
                  >
                    {{ fillForwardItemId() === item.id ? 'Cancel Fill' : 'Fill Forward' }}
                  </button>
                  <app-budget-item-delete
                    [budgetId]="budgetId()"
                    [categoryId]="categoryId()"
                    [itemId]="item.id"
                    (deleted)="onItemDeleted()"
                  />
                </div>
              </div>

              <app-budget-item-lifecycle
                [budgetId]="budgetId()"
                [categoryId]="categoryId()"
                [item]="item"
                (updated)="onLifecycleUpdated($event)"
              />

              @if (editingItemId() === item.id) {
                <div class="edit-panel" [attr.data-testid]="'edit-panel-' + item.id">
                  <app-budget-item-form
                    [budgetId]="budgetId()"
                    [categoryId]="categoryId()"
                    [item]="item"
                    (saved)="onItemSaved($event)"
                    (cancelled)="cancelEdit()"
                  />
                </div>
              }

              @if (fillForwardItemId() === item.id) {
                <div class="fill-forward-panel" [attr.data-testid]="'fill-forward-panel-' + item.id">
                  <app-budget-item-fill-forward
                    [budgetId]="budgetId()"
                    [categoryId]="categoryId()"
                    [item]="item"
                    (filled)="onFillForwardDone($event)"
                    (cancelled)="cancelFillForward()"
                  />
                </div>
              }
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [
    `
      .workspace {
        padding: 1rem;
      }

      .workspace-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
      }

      .header-actions {
        display: flex;
        gap: 0.5rem;
      }

      .category-title {
        margin: 0;
        font-size: 1.1rem;
      }

      .btn-add-item {
        padding: 0.4rem 0.9rem;
        background: #007bff;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.875rem;
      }

      .btn-add-item:disabled {
        opacity: 0.65;
        cursor: not-allowed;
      }

      .btn-bulk-create {
        padding: 0.4rem 0.9rem;
        background: #28a745;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.875rem;
      }

      .btn-bulk-create:disabled {
        opacity: 0.65;
        cursor: not-allowed;
      }

      .bulk-create-panel {
        margin-bottom: 1rem;
      }

      .single-create-panel {
        margin-bottom: 1rem;
      }

      .state-loading,
      .state-error,
      .state-empty {
        padding: 1rem;
        text-align: center;
        color: #6c757d;
        font-size: 0.9rem;
      }

      .state-error {
        color: #dc3545;
        background: #f8d7da;
        border-radius: 4px;
      }

      .items-list {
        list-style: none;
        margin: 0;
        padding: 0;
      }

      .item-row {
        border: 1px solid #dee2e6;
        border-radius: 6px;
        padding: 0.75rem;
        margin-bottom: 0.5rem;
        background: #fff;
      }

      .item-header {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        margin-bottom: 0.5rem;
      }

      .item-month {
        font-weight: 600;
        font-size: 0.9rem;
      }

      .item-flow {
        font-size: 0.8rem;
        padding: 0.15rem 0.4rem;
        border-radius: 3px;
        background: #e9ecef;
        color: #495057;
      }

      .item-actions {
        display: flex;
        gap: 0.4rem;
        margin-left: auto;
        align-items: center;
      }

      .btn-edit,
      .btn-fill-forward {
        padding: 0.25rem 0.6rem;
        border: 1px solid #007bff;
        background: white;
        color: #007bff;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.8rem;
      }

      .btn-edit:hover,
      .btn-fill-forward:hover {
        background: #007bff;
        color: white;
      }

      .edit-panel,
      .fill-forward-panel {
        margin-top: 0.75rem;
        padding-top: 0.75rem;
        border-top: 1px solid #dee2e6;
      }
    `,
  ],
})
export class BudgetItemsWorkspaceComponent {
  private readonly budgetItemApi = inject(BudgetItemApiService);

  readonly budgetId = input.required<string>();
  readonly categoryId = input.required<string>();
  readonly categoryName = input<string>('');

  readonly items = signal<BudgetItemDto[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);

  readonly editingItemId = signal<string | null>(null);
  readonly fillForwardItemId = signal<string | null>(null);
  readonly showBulkCreate = signal(false);
  readonly showSingleCreate = signal(false);

  constructor() {
    effect(() => {
      const budgetId = this.budgetId();
      const categoryId = this.categoryId();
      this.loadItems(budgetId, categoryId);
    });
  }

  loadItems(budgetId = this.budgetId(), categoryId = this.categoryId()): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.editingItemId.set(null);
    this.fillForwardItemId.set(null);
    this.showBulkCreate.set(false);

    this.budgetItemApi.listItems(budgetId, categoryId).subscribe((result) => {
      this.loading.set(false);
      if (isSuccess(result)) {
        this.items.set(result.value);
      } else {
        this.loadError.set(getErrorMessage(result.error));
      }
    });
  }

  toggleEdit(itemId: string): void {
    if (this.editingItemId() === itemId) {
      this.editingItemId.set(null);
    } else {
      this.editingItemId.set(itemId);
      this.fillForwardItemId.set(null);
    }
  }

  cancelEdit(): void {
    this.editingItemId.set(null);
  }

  toggleFillForward(itemId: string): void {
    if (this.fillForwardItemId() === itemId) {
      this.fillForwardItemId.set(null);
    } else {
      this.fillForwardItemId.set(itemId);
      this.editingItemId.set(null);
    }
  }

  cancelFillForward(): void {
    this.fillForwardItemId.set(null);
  }

  openBulkCreate(): void {
    this.showBulkCreate.set(true);
  }

  closeBulkCreate(): void {
    this.showBulkCreate.set(false);
  }

  openSingleCreate(): void {
    this.showSingleCreate.set(true);
  }

  closeSingleCreate(): void {
    this.showSingleCreate.set(false);
  }

  onItemSaved(_updated: BudgetItemDto): void {
    this.editingItemId.set(null);
    this.loadItems();
  }

  onItemDeleted(): void {
    this.loadItems();
  }

  onLifecycleUpdated(updated: BudgetItemDto): void {
    this.items.update((items) => items.map((i) => (i.id === updated.id ? updated : i)));
  }

  onFillForwardDone(_items: BudgetItemDto[]): void {
    this.fillForwardItemId.set(null);
    this.loadItems();
  }

  onBulkCreateSaved(_items: BudgetItemDto[]): void {
    this.showBulkCreate.set(false);
    this.loadItems();
  }

  onSingleCreateSaved(_item: BudgetItemDto): void {
    this.showSingleCreate.set(false);
    this.loadItems();
  }
}
