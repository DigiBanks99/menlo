import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  BudgetItemApiService,
  BudgetItemDto,
  RealizeBudgetItemRequest,
  RecordBudgetItemSpentRequest,
} from 'data-access-menlo-api';
import {
  MnlAmountInputComponent,
  MnlButtonComponent,
  MnlFormFieldComponent,
  MnlToastService,
} from 'menlo-lib';
import { getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-item-lifecycle',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MnlAmountInputComponent,
    MnlButtonComponent,
    MnlFormFieldComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-4">
      <div class="grid gap-3 sm:grid-cols-3">
        <div class="rounded-2xl border border-mnl-border/70 bg-mnl-surface-alt/50 p-3">
          <span class="label text-xs font-semibold uppercase tracking-wide text-mnl-subtext">
            Planned
          </span>
          <span
            class="value mt-1 block text-base font-semibold text-mnl-text"
            data-testid="planned-amount"
          >
            {{ item().plannedCurrency }} {{ item().plannedAmount }}
          </span>
        </div>

        @if (item().realizedAmount != null) {
          <div class="rounded-2xl border border-mnl-border/70 bg-mnl-surface-alt/50 p-3">
            <span class="label text-xs font-semibold uppercase tracking-wide text-mnl-subtext">
              Realized
            </span>
            <span
              class="value mt-1 block text-base font-semibold text-mnl-text"
              data-testid="realized-amount"
            >
              {{ item().realizedCurrency }} {{ item().realizedAmount }}
            </span>
          </div>
        }

        @if (item().spentAmount != null) {
          <div class="rounded-2xl border border-mnl-border/70 bg-mnl-surface-alt/50 p-3">
            <span class="label text-xs font-semibold uppercase tracking-wide text-mnl-subtext">
              Spent
            </span>
            <span
              class="value mt-1 block text-base font-semibold text-mnl-text"
              data-testid="spent-amount"
            >
              {{ item().spentCurrency }} {{ item().spentAmount }}
            </span>
          </div>
        }
      </div>

      @if (mode() === 'idle') {
        <div class="actions flex flex-col gap-3 sm:flex-row">
          <mnl-button
            testId="btn-realize"
            type="button"
            variant="secondary"
            (pressed)="startRealize()"
          >
            Record bill
          </mnl-button>
          <mnl-button testId="btn-spent" type="button" variant="ghost" (pressed)="startSpent()">
            Record payment
          </mnl-button>
        </div>
      } @else {
        <form [formGroup]="amountForm" (ngSubmit)="onSubmit()" class="amount-form space-y-4">
          <mnl-form-field
            inputId="amount"
            label="{{ mode() === 'realize' ? 'Bill amount' : 'Payment amount' }}"
            [error]="amountErrorMessage()"
          >
            <mnl-amount-input id="amount" testId="input-amount" formControlName="amount" />
          </mnl-form-field>

          @if (error()) {
            <div
              class="rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
              data-testid="error-message"
            >
              {{ error() }}
            </div>
          }

          <div class="form-actions flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <mnl-button
              testId="btn-cancel"
              type="button"
              variant="ghost"
              [disabled]="saving()"
              (pressed)="cancel()"
            >
              Cancel
            </mnl-button>

            <mnl-button testId="btn-submit" type="submit" [loading]="saving()">
              {{ saving() ? 'Saving...' : 'Save' }}
            </mnl-button>
          </div>
        </form>
      }
    </div>
  `,
})
export class BudgetItemLifecycleComponent {
  private readonly budgetItemApi = inject(BudgetItemApiService);
  private readonly toastService = inject(MnlToastService);

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
    this.error.set(null);

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
        this.toastService.show(this.mode() === 'realize' ? 'Bill recorded.' : 'Payment recorded.', {
          variant: 'success',
        });
        this.updated.emit(result.value);
        this.mode.set('idle');
      } else {
        const message = getErrorMessage(result.error);
        this.error.set(message);
        this.toastService.show(message, { variant: 'error' });
      }
    });
  }

  protected amountErrorMessage(): string | null {
    const control = this.amountForm.controls.amount;
    if (!control.touched || !control.errors) {
      return null;
    }

    return 'Amount is required and must be positive';
  }

  private resetForm(): void {
    this.amountForm.reset();
    this.error.set(null);
  }
}
