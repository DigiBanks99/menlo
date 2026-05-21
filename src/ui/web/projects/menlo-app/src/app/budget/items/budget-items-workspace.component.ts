import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { BudgetItemApiService, BudgetItemDto } from 'data-access-menlo-api';
import {
  MnlBadgeComponent,
  type MnlBadgeVariant,
  MnlButtonComponent,
  MnlCardComponent,
  MnlPanelComponent,
  MnlToastService,
} from 'menlo-lib';
import { getErrorMessage, isSuccess } from 'shared-util';
import { BudgetItemBulkCreateComponent } from './budget-item-bulk-create.component';
import { BudgetItemDeleteComponent } from './budget-item-delete.component';
import { BudgetItemFillForwardComponent } from './budget-item-fill-forward.component';
import { BudgetItemFormComponent } from './budget-item-form.component';
import { BudgetItemLifecycleComponent } from './budget-item-lifecycle.component';

@Component({
  selector: 'app-budget-items-workspace',
  standalone: true,
  imports: [
    MnlBadgeComponent,
    MnlButtonComponent,
    MnlCardComponent,
    MnlPanelComponent,
    BudgetItemLifecycleComponent,
    BudgetItemFormComponent,
    BudgetItemDeleteComponent,
    BudgetItemFillForwardComponent,
    BudgetItemBulkCreateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4" data-testid="budget-items-workspace">
      <mnl-card padding="lg">
        <div
          mnlCardHeader
          class="workspace-header flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between"
        >
          <div class="space-y-1">
            @if (categoryName()) {
              <h3
                class="category-title m-0 text-xl font-semibold text-mnl-text"
                data-testid="category-title"
              >
                {{ categoryName() }}
              </h3>
            }
            <p class="m-0 text-sm text-mnl-subtext">
              Manage the monthly line items and supporting actions for this category.
            </p>
          </div>

          <div class="header-actions flex flex-col gap-3 sm:flex-row">
            <mnl-button
              testId="btn-open-single-create"
              type="button"
              [disabled]="showSingleCreate()"
              (pressed)="openSingleCreate()"
            >
              Add item
            </mnl-button>

            <mnl-button
              testId="btn-open-bulk-create"
              type="button"
              variant="secondary"
              [disabled]="showBulkCreate()"
              (pressed)="openBulkCreate()"
            >
              Add line items
            </mnl-button>
          </div>
        </div>

        @if (loading()) {
          <div
            class="state-loading py-10 text-center text-sm text-mnl-subtext"
            data-testid="state-loading"
          >
            Loading items…
          </div>
        } @else if (loadError()) {
          <div
            class="state-error rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
            data-testid="state-error"
          >
            {{ loadError() }}
          </div>
        } @else if (items().length === 0) {
          <div
            class="state-empty py-10 text-center text-sm text-mnl-subtext"
            data-testid="state-empty"
          >
            No items for this category yet.
          </div>
        } @else {
          <ul class="items-list space-y-4" data-testid="items-list">
            @for (item of items(); track item.id) {
              <li class="item-row space-y-3" [attr.data-testid]="'item-row-' + item.id">
                <mnl-card>
                  <div
                    class="item-header flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between"
                  >
                    <div class="space-y-2">
                      <div class="flex flex-wrap items-center gap-2">
                        <span
                          class="item-month text-base font-semibold text-mnl-text"
                          data-testid="item-month"
                        >
                          Month {{ item.month }}
                        </span>
                        <span class="item-flow" data-testid="item-flow">
                          <mnl-badge size="sm" [variant]="flowVariantFor(item.budgetFlow)">
                            {{ item.budgetFlow }}
                          </mnl-badge>
                        </span>
                      </div>
                      <p class="m-0 text-sm text-mnl-subtext">
                        Edit the line item, repeat it through year-end, or record realized and spent
                        amounts.
                      </p>
                    </div>

                    <div class="item-actions flex flex-wrap justify-end gap-2">
                      <mnl-button
                        size="sm"
                        type="button"
                        variant="ghost"
                        [testId]="'btn-edit-' + item.id"
                        (pressed)="toggleEdit(item.id)"
                      >
                        {{ editingItemId() === item.id ? 'Cancel edit' : 'Edit' }}
                      </mnl-button>

                      <mnl-button
                        size="sm"
                        type="button"
                        variant="secondary"
                        [testId]="'btn-fill-forward-' + item.id"
                        (pressed)="toggleFillForward(item.id)"
                      >
                        {{ fillForwardItemId() === item.id ? 'Cancel fill' : 'Fill forward' }}
                      </mnl-button>

                      <app-budget-item-delete
                        [budgetId]="budgetId()"
                        [categoryId]="categoryId()"
                        [itemId]="item.id"
                        (deleted)="onItemDeleted()"
                      />
                    </div>
                  </div>

                  <div class="mt-4">
                    <app-budget-item-lifecycle
                      [budgetId]="budgetId()"
                      [categoryId]="categoryId()"
                      [item]="item"
                      (updated)="onLifecycleUpdated($event)"
                    />
                  </div>
                </mnl-card>

                @if (editingItemId() === item.id) {
                  <div class="edit-panel">
                    <mnl-panel
                      [open]="true"
                      [rootTestId]="'edit-panel-' + item.id"
                      (closed)="cancelEdit()"
                    >
                      <div mnlPanelHeader class="space-y-1">
                        <h3 class="m-0 text-xl font-semibold text-mnl-text">
                          Edit Month {{ item.month }}
                        </h3>
                        <p class="m-0 text-sm text-mnl-subtext">
                          Update the line item while keeping its API behavior unchanged.
                        </p>
                      </div>

                      <app-budget-item-form
                        [budgetId]="budgetId()"
                        [categoryId]="categoryId()"
                        [item]="item"
                        (saved)="onItemSaved($event)"
                        (cancelled)="cancelEdit()"
                      />
                    </mnl-panel>
                  </div>
                }

                @if (fillForwardItemId() === item.id) {
                  <div class="fill-forward-panel">
                    <mnl-panel
                      [open]="true"
                      [rootTestId]="'fill-forward-panel-' + item.id"
                      (closed)="cancelFillForward()"
                    >
                      <div mnlPanelHeader class="space-y-1">
                        <h3 class="m-0 text-xl font-semibold text-mnl-text">
                          Fill forward from Month {{ item.month }}
                        </h3>
                        <p class="m-0 text-sm text-mnl-subtext">
                          Copy the line item through the rest of the year with the same splits.
                        </p>
                      </div>

                      <app-budget-item-fill-forward
                        [budgetId]="budgetId()"
                        [categoryId]="categoryId()"
                        [item]="item"
                        (filled)="onFillForwardDone($event)"
                        (cancelled)="cancelFillForward()"
                      />
                    </mnl-panel>
                  </div>
                }
              </li>
            }
          </ul>
        }
      </mnl-card>

      @if (showSingleCreate()) {
        <div class="single-create-panel">
          <mnl-panel [open]="true" rootTestId="single-create-panel" (closed)="closeSingleCreate()">
            <div mnlPanelHeader class="space-y-1">
              <h3 class="m-0 text-xl font-semibold text-mnl-text">Add a budget item</h3>
              <p class="m-0 text-sm text-mnl-subtext">
                Capture a single monthly item for this category.
              </p>
            </div>

            <app-budget-item-form
              [budgetId]="budgetId()"
              [categoryId]="categoryId()"
              (saved)="onSingleCreateSaved($event)"
              (cancelled)="closeSingleCreate()"
            />
          </mnl-panel>
        </div>
      }

      @if (showBulkCreate()) {
        <div class="bulk-create-panel">
          <mnl-panel [open]="true" rootTestId="bulk-create-panel" (closed)="closeBulkCreate()">
            <div mnlPanelHeader class="space-y-1">
              <h3 class="m-0 text-xl font-semibold text-mnl-text">Add line items for all months</h3>
              <p class="m-0 text-sm text-mnl-subtext">
                Create the same item across every month of the year in one workflow.
              </p>
            </div>

            <app-budget-item-bulk-create
              [budgetId]="budgetId()"
              [categoryId]="categoryId()"
              (saved)="onBulkCreateSaved($event)"
              (cancelled)="closeBulkCreate()"
            />
          </mnl-panel>
        </div>
      }
    </div>
  `,
})
export class BudgetItemsWorkspaceComponent {
  private readonly budgetItemApi = inject(BudgetItemApiService);
  private readonly toastService = inject(MnlToastService);

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
        const message = getErrorMessage(result.error);
        this.loadError.set(message);
        this.toastService.show(message, { variant: 'error' });
      }
    });
  }

  toggleEdit(itemId: string): void {
    if (this.editingItemId() === itemId) {
      this.editingItemId.set(null);
      return;
    }

    this.editingItemId.set(itemId);
    this.fillForwardItemId.set(null);
  }

  cancelEdit(): void {
    this.editingItemId.set(null);
  }

  toggleFillForward(itemId: string): void {
    if (this.fillForwardItemId() === itemId) {
      this.fillForwardItemId.set(null);
      return;
    }

    this.fillForwardItemId.set(itemId);
    this.editingItemId.set(null);
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
    this.items.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
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

  protected flowVariantFor(flow: BudgetItemDto['budgetFlow']): MnlBadgeVariant {
    return flow === 'Income' ? 'success' : 'info';
  }
}
