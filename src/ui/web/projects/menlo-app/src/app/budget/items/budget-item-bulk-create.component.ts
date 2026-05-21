import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import {
  AttributionAllocationDto,
  BudgetItemApiService,
  BudgetItemDto,
  BulkCreateBudgetItemRequest,
  PayerAllocationDto,
} from 'data-access-menlo-api';
import {
  MnlAmountInputComponent,
  MnlButtonComponent,
  MnlFormFieldComponent,
  MnlFormLayoutComponent,
  MnlInputComponent,
  type MnlSelectOption,
  MnlSelectComponent,
  MnlToastService,
} from 'menlo-lib';
import {
  ApiError,
  getErrorMessage,
  hasValidationErrors,
  isSuccess,
  mapValidationErrorsToForm,
} from 'shared-util';

type PayerSplitGroup = FormGroup<{
  userId: FormControl<string>;
  percent: FormControl<number>;
}>;

type AttributionSplitGroup = FormGroup<{
  attribution: FormControl<string>;
  percent: FormControl<number>;
}>;

const budgetFlowOptions: readonly MnlSelectOption[] = [
  { value: 'Income', label: 'Income' },
  { value: 'Expense', label: 'Expense' },
];

const attributionOptions: readonly MnlSelectOption[] = [
  { value: 'Main', label: 'Main' },
  { value: 'Rental', label: 'Rental' },
  { value: 'ServiceProvider', label: 'Service Provider' },
];

function splitSumValidator(control: AbstractControl): ValidationErrors | null {
  const array = control as FormArray;
  const sum = array.controls.reduce((acc, group) => {
    const percent = (group as FormGroup).controls['percent']?.value ?? 0;
    return acc + percent;
  }, 0);

  return Math.abs(sum - 100) < 0.001 ? null : { splitSum: { actual: sum, required: 100 } };
}

@Component({
  selector: 'app-budget-item-bulk-create',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MnlAmountInputComponent,
    MnlButtonComponent,
    MnlFormFieldComponent,
    MnlFormLayoutComponent,
    MnlInputComponent,
    MnlSelectComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="block" data-testid="bulk-create-form">
      <mnl-form-layout>
        <div mnlFormTitle class="space-y-2">
          <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-accent">
            Bulk create
          </p>
          <h3 class="m-0 text-2xl font-bold tracking-tight text-mnl-text">
            Add line items for all 12 months
          </h3>
          <p class="m-0 text-sm leading-6 text-mnl-subtext">
            Create the same line item across the full year with shared payer and attribution splits.
          </p>
        </div>

        <div class="space-y-6">
          <section class="grid gap-4 md:grid-cols-2">
            <mnl-form-field
              errorTestId="error-budgetFlow"
              inputId="bulk-budgetFlow"
              label="Type"
              [error]="budgetFlowErrorMessage()"
              [required]="true"
            >
              <mnl-select
                id="bulk-budgetFlow"
                placeholder="-- Select --"
                testId="select-budget-flow"
                [options]="budgetFlowOptions"
                formControlName="budgetFlow"
              />
            </mnl-form-field>

            <mnl-form-field
              errorTestId="error-amount"
              inputId="amount"
              label="Monthly amount"
              [error]="amountErrorMessage()"
              [required]="true"
            >
              <mnl-amount-input id="amount" testId="input-amount" formControlName="amount" />
            </mnl-form-field>
          </section>

          <section
            class="space-y-4 rounded-2xl border border-mnl-border/80 bg-mnl-surface-alt/40 p-4"
            formArrayName="payerSplit"
          >
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div class="space-y-1">
                <h4 class="m-0 text-sm font-semibold text-mnl-text">Payer split</h4>
                <p class="m-0 text-sm text-mnl-subtext">
                  Every generated item will inherit this payer allocation.
                </p>
              </div>

              <span [class]="splitTotalClasses(payerSplitTotal())" data-testid="payer-split-total">
                {{ payerSplitTotal() }}%
              </span>
            </div>

            <div class="space-y-3">
              @for (payer of payerSplitControls; track $index) {
                <div
                  class="grid gap-3 rounded-2xl border border-mnl-border/70 bg-mnl-surface p-3 md:grid-cols-[minmax(0,1fr)_7rem_auto]"
                  [formGroupName]="$index"
                >
                  <mnl-input
                    placeholder="User ID"
                    [testId]="'input-payer-userId-' + $index"
                    formControlName="userId"
                  />

                  <mnl-input
                    placeholder="%"
                    type="number"
                    [testId]="'input-payer-percent-' + $index"
                    formControlName="percent"
                  />

                  <mnl-button
                    size="sm"
                    type="button"
                    variant="ghost"
                    [testId]="'btn-remove-payer-' + $index"
                    (pressed)="removePayerSplit($index)"
                  >
                    Remove
                  </mnl-button>
                </div>
              }
            </div>

            <mnl-button
              size="sm"
              type="button"
              variant="secondary"
              testId="btn-add-payer"
              (pressed)="addPayerSplit()"
            >
              Add payer
            </mnl-button>

            @if (form.controls.payerSplit.errors?.['splitSum']) {
              <p class="m-0 text-sm font-medium text-mnl-error" data-testid="error-payerSplit">
                Payer split must total 100% (currently {{ payerSplitTotal() }}%)
              </p>
            }
          </section>

          <section
            class="space-y-4 rounded-2xl border border-mnl-border/80 bg-mnl-surface-alt/40 p-4"
            formArrayName="attributionSplit"
          >
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div class="space-y-1">
                <h4 class="m-0 text-sm font-semibold text-mnl-text">Attribution split</h4>
                <p class="m-0 text-sm text-mnl-subtext">
                  Apply a consistent attribution split to every month created.
                </p>
              </div>

              <span
                [class]="splitTotalClasses(attributionSplitTotal())"
                data-testid="attribution-split-total"
              >
                {{ attributionSplitTotal() }}%
              </span>
            </div>

            <div class="space-y-3">
              @for (attr of attributionSplitControls; track $index) {
                <div
                  class="grid gap-3 rounded-2xl border border-mnl-border/70 bg-mnl-surface p-3 md:grid-cols-[minmax(0,1fr)_7rem_auto]"
                  [formGroupName]="$index"
                >
                  <mnl-select
                    placeholder="-- Select --"
                    [options]="attributionOptions"
                    [testId]="'select-attribution-' + $index"
                    formControlName="attribution"
                  />

                  <mnl-input
                    placeholder="%"
                    type="number"
                    [testId]="'input-attribution-percent-' + $index"
                    formControlName="percent"
                  />

                  <mnl-button
                    size="sm"
                    type="button"
                    variant="ghost"
                    [testId]="'btn-remove-attribution-' + $index"
                    (pressed)="removeAttributionSplit($index)"
                  >
                    Remove
                  </mnl-button>
                </div>
              }
            </div>

            <mnl-button
              size="sm"
              type="button"
              variant="secondary"
              testId="btn-add-attribution"
              (pressed)="addAttributionSplit()"
            >
              Add attribution
            </mnl-button>

            @if (form.controls.attributionSplit.errors?.['splitSum']) {
              <p
                class="m-0 text-sm font-medium text-mnl-error"
                data-testid="error-attributionSplit"
              >
                Attribution split must total 100% (currently {{ attributionSplitTotal() }}%)
              </p>
            }
          </section>

          @if (formError()) {
            <div
              class="rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
              data-testid="form-error"
            >
              {{ formError() }}
            </div>
          }
        </div>

        <div mnlFormActions class="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
          <mnl-button type="button" variant="ghost" testId="btn-cancel" (pressed)="onCancel()">
            Cancel
          </mnl-button>

          <mnl-button type="submit" [loading]="saving()" testId="btn-submit">
            {{ saving() ? 'Creating...' : 'Create all 12 months' }}
          </mnl-button>
        </div>
      </mnl-form-layout>
    </form>
  `,
})
export class BudgetItemBulkCreateComponent implements OnInit {
  private readonly budgetItemApi = inject(BudgetItemApiService);
  private readonly toastService = inject(MnlToastService);

  readonly budgetId = input.required<string>();
  readonly categoryId = input.required<string>();

  readonly saved = output<BudgetItemDto[]>();
  readonly cancelled = output<void>();

  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);

  protected readonly attributionOptions = attributionOptions;
  protected readonly budgetFlowOptions = budgetFlowOptions;

  readonly form = new FormGroup({
    budgetFlow: new FormControl<'Income' | 'Expense' | ''>('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    amount: new FormControl<number | null>(null, {
      validators: [Validators.required, Validators.min(0.01)],
    }),
    payerSplit: new FormArray<PayerSplitGroup>([], { validators: [splitSumValidator] }),
    attributionSplit: new FormArray<AttributionSplitGroup>([], {
      validators: [splitSumValidator],
    }),
  });

  get payerSplitControls() {
    return this.form.controls.payerSplit.controls;
  }

  get attributionSplitControls() {
    return this.form.controls.attributionSplit.controls;
  }

  private readonly _splitChange = signal(0);

  readonly payerSplitTotal = computed(() => {
    this._splitChange();
    return this.form.controls.payerSplit.controls.reduce(
      (sum, group) => sum + (group.controls.percent.value ?? 0),
      0,
    );
  });

  readonly attributionSplitTotal = computed(() => {
    this._splitChange();
    return this.form.controls.attributionSplit.controls.reduce(
      (sum, group) => sum + (group.controls.percent.value ?? 0),
      0,
    );
  });

  ngOnInit(): void {
    this.form.controls.payerSplit.valueChanges.subscribe(() => this.notifySplitChange());
    this.form.controls.attributionSplit.valueChanges.subscribe(() => this.notifySplitChange());
  }

  addPayerSplit(userId = '', percent = 0): void {
    this.form.controls.payerSplit.push(
      new FormGroup({
        userId: new FormControl(userId, { nonNullable: true, validators: [Validators.required] }),
        percent: new FormControl(percent, { nonNullable: true, validators: [Validators.required] }),
      }),
    );
    this.notifySplitChange();
  }

  removePayerSplit(index: number): void {
    this.form.controls.payerSplit.removeAt(index);
    this.notifySplitChange();
  }

  addAttributionSplit(attribution = '', percent = 0): void {
    this.form.controls.attributionSplit.push(
      new FormGroup({
        attribution: new FormControl(attribution, {
          nonNullable: true,
          validators: [Validators.required],
        }),
        percent: new FormControl(percent, { nonNullable: true, validators: [Validators.required] }),
      }),
    );
    this.notifySplitChange();
  }

  removeAttributionSplit(index: number): void {
    this.form.controls.attributionSplit.removeAt(index);
    this.notifySplitChange();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    const value = this.form.getRawValue();
    const request: BulkCreateBudgetItemRequest = {
      budgetFlow: value.budgetFlow as 'Income' | 'Expense',
      amount: value.amount!,
      currency: 'ZAR',
      payerSplit: value.payerSplit as PayerAllocationDto[],
      attributionSplit: value.attributionSplit as unknown as AttributionAllocationDto[],
    };

    this.budgetItemApi
      .bulkCreateItems(this.budgetId(), this.categoryId(), request)
      .subscribe((result) => {
        this.saving.set(false);
        if (isSuccess(result)) {
          this.toastService.show('Budget items created for all 12 months.', { variant: 'success' });
          this.saved.emit(result.value);
        } else {
          this.handleError(result.error);
        }
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

    if (typeof control.errors['api'] === 'string') {
      return control.errors['api'] as string;
    }

    return 'Amount must be positive';
  }

  protected budgetFlowErrorMessage(): string | null {
    const control = this.form.controls.budgetFlow;
    if (!control.touched || !control.errors) {
      return null;
    }

    if (typeof control.errors['api'] === 'string') {
      return control.errors['api'] as string;
    }

    return 'Required';
  }

  protected splitTotalClasses(total: number): string {
    const baseClasses = 'inline-flex rounded-full px-3 py-1 text-xs font-semibold';
    return total === 100
      ? `${baseClasses} bg-mnl-success/20 text-mnl-success`
      : `${baseClasses} bg-mnl-error/15 text-mnl-error`;
  }

  private handleError(error: ApiError): void {
    const message = getErrorMessage(error);
    if (hasValidationErrors(error)) {
      mapValidationErrorsToForm(error, this.form);
      this.toastService.show('Please fix the highlighted validation errors.', {
        variant: 'warning',
      });
    } else {
      this.formError.set(message);
      this.toastService.show(message, { variant: 'error' });
    }
  }

  private notifySplitChange(): void {
    this._splitChange.update((value) => value + 1);
  }
}
