import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { BudgetItemApiService } from 'data-access-menlo-api';
import { MnlButtonComponent, MnlPanelComponent, MnlToastService } from 'menlo-lib';
import { getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-item-delete',
  standalone: true,
  imports: [MnlButtonComponent, MnlPanelComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!confirming()) {
      <mnl-button
        size="sm"
        testId="btn-delete"
        type="button"
        variant="destructive"
        (pressed)="askConfirmation()"
      >
        Delete
      </mnl-button>
    } @else {
      <div>
        <mnl-panel
          [open]="confirming()"
          mode="dialog"
          rootTestId="delete-confirmation-panel"
          (closed)="cancelDelete()"
        >
          <div mnlPanelHeader class="space-y-1">
            <h3 class="m-0 text-xl font-semibold text-mnl-text">Delete budget item?</h3>
            <p class="m-0 text-sm text-mnl-subtext">
              This action cannot be undone from the current workspace.
            </p>
          </div>

          <div class="space-y-4">
            <p class="m-0 text-sm leading-6 text-mnl-text">Are you sure?</p>

            @if (error()) {
              <div
                class="rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
                data-testid="delete-error"
              >
                {{ error() }}
              </div>
            }

            <div class="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
              <mnl-button
                testId="btn-confirm-no"
                type="button"
                variant="ghost"
                [disabled]="deleting()"
                (pressed)="cancelDelete()"
              >
                No
              </mnl-button>

              <mnl-button
                testId="btn-confirm-yes"
                type="button"
                variant="destructive"
                [loading]="deleting()"
                (pressed)="confirmDelete()"
              >
                {{ deleting() ? 'Deleting...' : 'Yes, delete' }}
              </mnl-button>
            </div>
          </div>
        </mnl-panel>
      </div>
    }
  `,
})
export class BudgetItemDeleteComponent {
  private readonly budgetItemApi = inject(BudgetItemApiService);
  private readonly toastService = inject(MnlToastService);

  readonly budgetId = input.required<string>();
  readonly categoryId = input.required<string>();
  readonly itemId = input.required<string>();
  readonly deleted = output<void>();

  readonly confirming = signal(false);
  readonly deleting = signal(false);
  readonly error = signal<string | null>(null);

  askConfirmation(): void {
    this.confirming.set(true);
    this.error.set(null);
  }

  cancelDelete(): void {
    if (this.deleting()) {
      return;
    }

    this.confirming.set(false);
    this.error.set(null);
  }

  confirmDelete(): void {
    this.deleting.set(true);
    this.budgetItemApi
      .deleteItem(this.budgetId(), this.categoryId(), this.itemId())
      .subscribe((result) => {
        this.deleting.set(false);
        if (isSuccess(result)) {
          this.toastService.show('Budget item deleted.', { variant: 'success' });
          this.confirming.set(false);
          this.deleted.emit();
        } else {
          const message = getErrorMessage(result.error);
          this.error.set(message);
          this.toastService.show(message, { variant: 'error' });
        }
      });
  }
}
