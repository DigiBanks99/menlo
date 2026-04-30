import { Component, inject, input, output, signal } from '@angular/core';
import { BudgetItemApiService } from 'data-access-menlo-api';
import { getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-item-delete',
  imports: [],
  template: `
    @if (!confirming()) {
      <button type="button" class="delete-btn" (click)="askConfirmation()" data-testid="btn-delete">
        Delete
      </button>
    } @else {
      <div class="confirm-prompt">
        <span>Are you sure?</span>
        <button
          type="button"
          class="confirm-yes"
          (click)="confirmDelete()"
          [disabled]="deleting()"
          data-testid="btn-confirm-yes"
        >
          {{ deleting() ? 'Deleting...' : 'Yes, delete' }}
        </button>
        <button
          type="button"
          class="confirm-no"
          (click)="cancelDelete()"
          [disabled]="deleting()"
          data-testid="btn-confirm-no"
        >
          No
        </button>
      </div>
      @if (error()) {
        <div class="error-message" data-testid="delete-error">{{ error() }}</div>
      }
    }
  `,
  styles: [
    `
      .delete-btn {
        padding: 0.3rem 0.6rem;
        border: 1px solid #dc3545;
        background: white;
        color: #dc3545;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.85rem;
      }

      .delete-btn:hover {
        background: #dc3545;
        color: white;
      }

      .confirm-prompt {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-size: 0.85rem;
      }

      .confirm-prompt span {
        color: #dc3545;
        font-weight: 500;
      }

      .confirm-yes {
        padding: 0.25rem 0.5rem;
        border: none;
        background: #dc3545;
        color: white;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.8rem;
      }

      .confirm-yes:disabled {
        background: #6c757d;
      }

      .confirm-no {
        padding: 0.25rem 0.5rem;
        border: 1px solid #ced4da;
        background: white;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.8rem;
      }

      .error-message {
        color: #dc3545;
        font-size: 0.8rem;
        margin-top: 0.25rem;
      }
    `,
  ],
})
export class BudgetItemDeleteComponent {
  private readonly budgetItemApi = inject(BudgetItemApiService);

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
          this.deleted.emit();
        } else {
          this.error.set(getErrorMessage(result.error));
        }
      });
  }
}
