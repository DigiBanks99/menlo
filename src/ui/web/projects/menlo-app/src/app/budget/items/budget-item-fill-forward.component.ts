import { Component, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  BudgetItemApiService,
  BudgetItemDto,
  FillForwardBudgetItemRequest,
} from 'data-access-menlo-api';
import { getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-item-fill-forward',
  imports: [ReactiveFormsModule],
  template: `
    <div class="fill-forward-info" data-testid="fill-forward-info">
      Fill forward {{ item().plannedCurrency }} {{ item().plannedAmount }} from Month
      {{ item().month }} through December?
    </div>

    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="fill-forward-form">
      <div class="form-field">
        <label for="amount">Amount</label>
        <input
          id="amount"
          type="number"
          formControlName="amount"
          step="0.01"
          data-testid="input-amount"
        />
        @if (form.controls.amount.touched && form.controls.amount.errors) {
          <span class="field-error">Amount is required and must be positive</span>
        }
      </div>

      @if (error()) {
        <div class="error-banner" data-testid="error-message">{{ error() }}</div>
      }

      <div class="form-actions">
        <button type="submit" [disabled]="saving()" data-testid="btn-submit">
          {{ saving() ? 'Filling...' : 'Fill Forward' }}
        </button>
        <button type="button" (click)="onCancel()" data-testid="btn-cancel">Cancel</button>
      </div>
    </form>
  `,
  styles: [
    `
      .fill-forward-info {
        margin-bottom: 0.75rem;
        font-weight: 500;
      }

      .fill-forward-form {
        display: flex;
        align-items: flex-end;
        gap: 0.5rem;
        flex-wrap: wrap;
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
        width: 100%;
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
        background: #007bff;
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
export class BudgetItemFillForwardComponent {
  private readonly budgetItemApi = inject(BudgetItemApiService);

  readonly budgetId = input.required<string>();
  readonly categoryId = input.required<string>();
  readonly item = input.required<BudgetItemDto>();
  readonly filled = output<BudgetItemDto[]>();
  readonly cancelled = output<void>();

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = new FormGroup({
    amount: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(0.01)],
    }),
  });

  constructor() {
    // Amount will be initialized via ngOnInit equivalent - we set it after input is available
  }

  ngOnInit(): void {
    this.form.controls.amount.setValue(this.item().plannedAmount);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const currentItem = this.item();
    const request: FillForwardBudgetItemRequest = {
      fromMonth: currentItem.month,
      budgetFlow: currentItem.budgetFlow,
      amount: this.form.controls.amount.value!,
      currency: currentItem.plannedCurrency,
      payerSplit: currentItem.payerSplit,
      attributionSplit: currentItem.attributionSplit,
    };

    this.budgetItemApi.fillForward(this.budgetId(), this.categoryId(), request).subscribe({
      next: (result) => {
        this.saving.set(false);
        if (isSuccess(result)) {
          this.filled.emit(result.value);
        } else {
          this.error.set(getErrorMessage(result.error));
        }
      },
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
