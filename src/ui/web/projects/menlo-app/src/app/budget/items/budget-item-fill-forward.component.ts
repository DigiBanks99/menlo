import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  BudgetItemApiService,
  BudgetItemDto,
  FillForwardBudgetItemRequest,
} from 'data-access-menlo-api';
import {
  MnlAmountInputComponent,
  MnlButtonComponent,
  MnlFormFieldComponent,
  MnlFormLayoutComponent,
  MnlToastService,
} from 'menlo-lib';
import { getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-item-fill-forward',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MnlAmountInputComponent,
    MnlButtonComponent,
    MnlFormFieldComponent,
    MnlFormLayoutComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="block">
      <mnl-form-layout>
        <div mnlFormTitle class="space-y-2">
          <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-accent">
            Fill forward
          </p>
          <h3 class="m-0 text-2xl font-bold tracking-tight text-mnl-text">Repeat this item</h3>
          <p class="m-0 text-sm leading-6 text-mnl-subtext" data-testid="fill-forward-info">
            Fill forward {{ item().plannedCurrency }} {{ item().plannedAmount }} from Month
            {{ item().month }} through December?
          </p>
        </div>

        <div class="space-y-4">
          <mnl-form-field
            inputId="fill-forward-amount"
            label="Amount"
            [error]="amountErrorMessage()"
          >
            <mnl-amount-input
              id="fill-forward-amount"
              testId="input-amount"
              formControlName="amount"
            />
          </mnl-form-field>

          @if (error()) {
            <div
              class="rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
              data-testid="error-message"
            >
              {{ error() }}
            </div>
          }
        </div>

        <div mnlFormActions class="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <mnl-button
            testId="btn-cancel"
            type="button"
            variant="ghost"
            [disabled]="saving()"
            (pressed)="onCancel()"
          >
            Cancel
          </mnl-button>

          <mnl-button testId="btn-submit" type="submit" [loading]="saving()">
            {{ saving() ? 'Filling...' : 'Fill forward' }}
          </mnl-button>
        </div>
      </mnl-form-layout>
    </form>
  `,
})
export class BudgetItemFillForwardComponent implements OnInit {
  private readonly budgetItemApi = inject(BudgetItemApiService);
  private readonly toastService = inject(MnlToastService);

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
          this.toastService.show('Budget item filled forward.', { variant: 'success' });
          this.filled.emit(result.value);
        } else {
          const message = getErrorMessage(result.error);
          this.error.set(message);
          this.toastService.show(message, { variant: 'error' });
        }
      },
    });
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  protected amountErrorMessage(): string | null {
    const control = this.form.controls.amount;
    if (!control.touched || !control.errors) {
      return null;
    }

    return 'Amount is required and must be positive';
  }
}
