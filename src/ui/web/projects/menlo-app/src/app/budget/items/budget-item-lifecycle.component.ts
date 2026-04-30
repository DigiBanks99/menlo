import { Component, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  BudgetItemApiService,
  BudgetItemDto,
  RealizeBudgetItemRequest,
  RecordBudgetItemSpentRequest,
} from 'data-access-menlo-api';
import { getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-item-lifecycle',
  imports: [ReactiveFormsModule],
  template: `
    <div class="lifecycle-summary">
      <div class="amount-group">
        <span class="label">Planned</span>
        <span class="value" data-testid="planned-amount"
          >{{ item().plannedCurrency }} {{ item().plannedAmount }}</span
        >
      </div>
      @if (item().realizedAmount != null) {
        <div class="amount-group">
          <span class="label">Realized</span>
          <span class="value" data-testid="realized-amount"
            >{{ item().realizedCurrency }} {{ item().realizedAmount }}</span
          >
        </div>
      }
      @if (item().spentAmount != null) {
        <div class="amount-group">
          <span class="label">Spent</span>
          <span class="value" data-testid="spent-amount"
            >{{ item().spentCurrency }} {{ item().spentAmount }}</span
          >
        </div>
      }
    </div>

    @if (mode() === 'idle') {
      <div class="actions">
        <button type="button" (click)="startRealize()" data-testid="btn-realize">
          Record Bill
        </button>
        <button type="button" (click)="startSpent()" data-testid="btn-spent">Record Payment</button>
      </div>
    } @else {
      <form [formGroup]="amountForm" (ngSubmit)="onSubmit()" class="amount-form">
        <div class="form-field">
          <label for="amount">{{ mode() === 'realize' ? 'Bill Amount' : 'Payment Amount' }}</label>
          <input
            id="amount"
            type="number"
            formControlName="amount"
            step="0.01"
            data-testid="input-amount"
          />
          @if (amountForm.controls.amount.touched && amountForm.controls.amount.errors) {
            <span class="field-error">Amount is required and must be positive</span>
          }
        </div>
        @if (error()) {
          <div class="error-banner" data-testid="error-message">{{ error() }}</div>
        }
        <div class="form-actions">
          <button type="submit" [disabled]="saving()" data-testid="btn-submit">
            {{ saving() ? 'Saving...' : 'Save' }}
          </button>
          <button type="button" (click)="cancel()" data-testid="btn-cancel">Cancel</button>
        </div>
      </form>
    }
  `,
  styles: [
    `
      .lifecycle-summary {
        display: flex;
        gap: 1.5rem;
        margin-bottom: 0.75rem;
      }

      .amount-group {
        display: flex;
        flex-direction: column;
      }

      .amount-group .label {
        font-size: 0.75rem;
        color: #6c757d;
        text-transform: uppercase;
      }

      .amount-group .value {
        font-weight: 600;
      }

      .actions {
        display: flex;
        gap: 0.5rem;
      }

      .actions button {
        padding: 0.4rem 0.8rem;
        border: 1px solid #007bff;
        background: white;
        color: #007bff;
        border-radius: 4px;
        cursor: pointer;
      }

      .actions button:hover {
        background: #007bff;
        color: white;
      }

      .amount-form {
        display: flex;
        align-items: flex-end;
        gap: 0.5rem;
        margin-top: 0.5rem;
      }

      .form-field {
        display: flex;
        flex-direction: column;
      }

      .form-field input {
        padding: 0.4rem 0.6rem;
        border: 1px solid #ced4da;
        border-radius: 4px;
        width: 150px;
      }

      .field-error {
        color: #dc3545;
        font-size: 0.8rem;
      }

      .error-banner {
        color: #dc3545;
        font-size: 0.85rem;
      }

      .form-actions {
        display: flex;
        gap: 0.5rem;
      }

      .form-actions button {
        padding: 0.4rem 0.8rem;
        border-radius: 4px;
        cursor: pointer;
      }

      .form-actions button[type='submit'] {
        background: #28a745;
        color: white;
        border: none;
      }

      .form-actions button[type='submit']:disabled {
        background: #6c757d;
      }

      .form-actions button[type='button'] {
        background: white;
        border: 1px solid #ced4da;
      }
    `,
  ],
})
export class BudgetItemLifecycleComponent {
  private readonly budgetItemApi = inject(BudgetItemApiService);

  readonly budgetId = input.required<string>();
  readonly categoryId = input.required<string>();
  readonly item = input.required<BudgetItemDto>();
  readonly updated = output<BudgetItemDto>();

  readonly mode = signal<'idle' | 'realize' | 'spent'>('idle');
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly amountForm = new FormGroup({
    amount: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(0.01)],
    }),
  });

  startRealize(): void {
    this.mode.set('realize');
    this.resetForm();
  }

  startSpent(): void {
    this.mode.set('spent');
    this.resetForm();
  }

  cancel(): void {
    this.mode.set('idle');
    this.error.set(null);
  }

  onSubmit(): void {
    if (this.amountForm.invalid) {
      this.amountForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const amount = this.amountForm.controls.amount.value!;
    const request: RealizeBudgetItemRequest | RecordBudgetItemSpentRequest = {
      amount,
      currency: 'ZAR',
    };

    const operation$ =
      this.mode() === 'realize'
        ? this.budgetItemApi.realizeItem(
            this.budgetId(),
            this.categoryId(),
            this.item().id,
            request,
          )
        : this.budgetItemApi.recordItemSpent(
            this.budgetId(),
            this.categoryId(),
            this.item().id,
            request,
          );

    operation$.subscribe((result) => {
      this.saving.set(false);
      if (isSuccess(result)) {
        this.updated.emit(result.value);
        this.mode.set('idle');
      } else {
        this.error.set(getErrorMessage(result.error));
      }
    });
  }

  private resetForm(): void {
    this.amountForm.reset();
    this.error.set(null);
  }
}
