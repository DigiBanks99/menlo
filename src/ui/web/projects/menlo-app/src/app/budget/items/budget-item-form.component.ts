import { Component, computed, inject, input, OnInit, output, signal } from '@angular/core';
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
  CreateBudgetItemRequest,
  PayerAllocationDto,
  UpdateBudgetItemRequest,
} from 'data-access-menlo-api';
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

function splitSumValidator(control: AbstractControl): ValidationErrors | null {
  const array = control as FormArray;
  const sum = array.controls.reduce((acc, group) => {
    const percent = (group as FormGroup).controls['percent']?.value ?? 0;
    return acc + percent;
  }, 0);
  return Math.abs(sum - 100) < 0.001 ? null : { splitSum: { actual: sum, required: 100 } };
}

@Component({
  selector: 'app-budget-item-form',
  imports: [ReactiveFormsModule],
  template: `
    <form
      [formGroup]="form"
      (ngSubmit)="onSubmit()"
      class="budget-item-form"
      data-testid="budget-item-form"
    >
      @if (isCreateMode()) {
        <div class="form-field">
          <label for="month">Month *</label>
          <input
            id="month"
            type="number"
            min="1"
            max="12"
            formControlName="month"
            data-testid="input-month"
          />
          @if (form.controls.month.touched && form.controls.month.errors) {
            <span class="field-error" data-testid="error-month">Month is required (1–12)</span>
          }
        </div>

        <div class="form-field">
          <label for="budgetFlow">Budget Flow *</label>
          <select id="budgetFlow" formControlName="budgetFlow" data-testid="select-budgetFlow">
            <option value="">-- Select --</option>
            <option value="Income">Income</option>
            <option value="Expense">Expense</option>
          </select>
          @if (form.controls.budgetFlow.touched && form.controls.budgetFlow.errors) {
            <span class="field-error" data-testid="error-budgetFlow">Budget flow is required</span>
          }
        </div>
      }

      <div class="form-field">
        <label for="plannedAmount">Planned Amount *</label>
        <input
          id="plannedAmount"
          type="number"
          formControlName="plannedAmount"
          data-testid="input-plannedAmount"
        />
        @if (form.controls.plannedAmount.touched && form.controls.plannedAmount.errors) {
          <span class="field-error" data-testid="error-plannedAmount">
            @if (form.controls.plannedAmount.errors['required']) {
              Planned amount is required
            } @else if (form.controls.plannedAmount.errors['api']) {
              {{ form.controls.plannedAmount.errors['api'] }}
            }
          </span>
        }
      </div>

      <div class="form-field">
        <label for="realizedAmount">Realized Amount</label>
        <input
          id="realizedAmount"
          type="number"
          formControlName="realizedAmount"
          data-testid="input-realizedAmount"
        />
      </div>

      <div class="form-field">
        <label for="spentAmount">Spent Amount</label>
        <input
          id="spentAmount"
          type="number"
          formControlName="spentAmount"
          data-testid="input-spentAmount"
        />
      </div>

      <fieldset class="split-section" formArrayName="payerSplit">
        <legend>
          Payer Split
          <span
            class="split-total"
            [class.invalid]="payerSplitTotal() !== 100"
            data-testid="payer-split-total"
          >
            ({{ payerSplitTotal() }}%)
          </span>
        </legend>
        @for (payer of payerSplitControls; track $index) {
          <div class="split-row" [formGroupName]="$index">
            <input
              placeholder="User ID"
              formControlName="userId"
              [attr.data-testid]="'input-payer-userId-' + $index"
            />
            <input
              type="number"
              placeholder="%"
              formControlName="percent"
              [attr.data-testid]="'input-payer-percent-' + $index"
            />
            <button
              type="button"
              class="btn-remove"
              (click)="removePayerSplit($index)"
              [attr.data-testid]="'btn-remove-payer-' + $index"
            >
              Remove
            </button>
          </div>
        }
        <button type="button" class="btn-add" (click)="addPayerSplit()" data-testid="btn-add-payer">
          Add Payer
        </button>
        @if (form.controls.payerSplit.errors?.['splitSum']) {
          <span class="field-error" data-testid="error-payerSplit">
            Payer split must total 100% (currently {{ payerSplitTotal() }}%)
          </span>
        }
      </fieldset>

      <fieldset class="split-section" formArrayName="attributionSplit">
        <legend>
          Attribution Split
          <span
            class="split-total"
            [class.invalid]="attributionSplitTotal() !== 100"
            data-testid="attribution-split-total"
          >
            ({{ attributionSplitTotal() }}%)
          </span>
        </legend>
        @for (attr of attributionSplitControls; track $index) {
          <div class="split-row" [formGroupName]="$index">
            <select formControlName="attribution" [attr.data-testid]="'select-attribution-' + $index">
              <option value="">-- Select --</option>
              <option value="Main">Main</option>
              <option value="Rental">Rental</option>
              <option value="ServiceProvider">ServiceProvider</option>
            </select>
            <input
              type="number"
              placeholder="%"
              formControlName="percent"
              [attr.data-testid]="'input-attribution-percent-' + $index"
            />
            <button
              type="button"
              class="btn-remove"
              (click)="removeAttributionSplit($index)"
              [attr.data-testid]="'btn-remove-attribution-' + $index"
            >
              Remove
            </button>
          </div>
        }
        <button
          type="button"
          class="btn-add"
          (click)="addAttributionSplit()"
          data-testid="btn-add-attribution"
        >
          Add Attribution
        </button>
        @if (form.controls.attributionSplit.errors?.['splitSum']) {
          <span class="field-error" data-testid="error-attributionSplit">
            Attribution split must total 100% (currently {{ attributionSplitTotal() }}%)
          </span>
        }
      </fieldset>

      @if (formError()) {
        <div class="error-banner" data-testid="form-error">{{ formError() }}</div>
      }

      <div class="form-actions">
        <button type="submit" class="btn-primary" [disabled]="saving()" data-testid="btn-save">
          {{ saving() ? 'Saving...' : isCreateMode() ? 'Create' : 'Update' }}
        </button>
        <button type="button" class="btn-secondary" (click)="onCancel()" data-testid="btn-cancel">
          Cancel
        </button>
      </div>
    </form>
  `,
  styles: [
    `
      .budget-item-form {
        padding: 1rem;
        border: 1px solid #dee2e6;
        border-radius: 6px;
        background: #f8f9fa;
        margin: 0.5rem 0;
      }

      .form-field {
        margin-bottom: 0.75rem;
      }

      .form-field label {
        display: block;
        font-weight: 500;
        margin-bottom: 0.25rem;
        font-size: 0.875rem;
      }

      .form-field input {
        width: 100%;
        padding: 0.4rem 0.6rem;
        border: 1px solid #ced4da;
        border-radius: 4px;
        font-size: 0.9rem;
      }

      .field-error {
        color: #dc3545;
        font-size: 0.8rem;
        margin-top: 0.2rem;
        display: block;
      }

      .error-banner {
        padding: 0.75rem;
        background: #f8d7da;
        border: 1px solid #f5c6cb;
        border-radius: 4px;
        color: #721c24;
        margin-bottom: 0.75rem;
        font-size: 0.875rem;
      }

      .split-section {
        border: 1px solid #ced4da;
        border-radius: 4px;
        padding: 0.75rem;
        margin-bottom: 0.75rem;
      }

      .split-section legend {
        font-weight: 500;
        font-size: 0.875rem;
        padding: 0 0.25rem;
      }

      .split-total {
        font-weight: normal;
        color: #28a745;
      }

      .split-total.invalid {
        color: #dc3545;
      }

      .split-row {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 0.5rem;
        align-items: center;
      }

      .split-row input,
      .split-row select {
        flex: 1;
        padding: 0.3rem 0.5rem;
        border: 1px solid #ced4da;
        border-radius: 4px;
        font-size: 0.85rem;
      }

      .split-row input[type='number'] {
        max-width: 80px;
      }

      .btn-add {
        padding: 0.3rem 0.75rem;
        background: #e9ecef;
        border: 1px solid #ced4da;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.8rem;
        margin-top: 0.25rem;
      }

      .btn-remove {
        padding: 0.2rem 0.5rem;
        background: #f8d7da;
        border: 1px solid #f5c6cb;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.8rem;
        color: #721c24;
      }

      .form-actions {
        display: flex;
        gap: 0.5rem;
      }

      .btn-primary {
        padding: 0.4rem 1rem;
        background: #007bff;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
      }

      .btn-primary:disabled {
        opacity: 0.65;
        cursor: not-allowed;
      }

      .btn-secondary {
        padding: 0.4rem 1rem;
        background: #6c757d;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
      }
    `,
  ],
})
export class BudgetItemFormComponent implements OnInit {
  private readonly budgetItemApi = inject(BudgetItemApiService);

  readonly budgetId = input.required<string>();
  readonly categoryId = input.required<string>();
  readonly item = input<BudgetItemDto | null>(null);

  readonly saved = output<BudgetItemDto>();
  readonly cancelled = output<void>();

  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);

  readonly isCreateMode = computed(() => this.item() == null);

  readonly form = new FormGroup({
    month: new FormControl<number>(1, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.max(12)],
    }),
    budgetFlow: new FormControl<'Income' | 'Expense' | ''>('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    plannedAmount: new FormControl<number>(0, {
      nonNullable: true,
      validators: [Validators.required],
    }),
    realizedAmount: new FormControl<number | null>(null),
    spentAmount: new FormControl<number | null>(null),
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

  private readonly _splitChange = signal(0);

  ngOnInit(): void {
    const existing = this.item();
    if (existing) {
      this.form.patchValue({
        month: existing.month,
        budgetFlow: existing.budgetFlow,
        plannedAmount: existing.plannedAmount,
        realizedAmount: existing.realizedAmount,
        spentAmount: existing.spentAmount,
      });

      for (const payer of existing.payerSplit) {
        this.addPayerSplit(payer.userId, payer.percent);
      }
      for (const attr of existing.attributionSplit) {
        this.addAttributionSplit(attr.attribution, attr.percent);
      }
    }

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

    const existing = this.item();
    if (existing) {
      this.doUpdate(existing);
    } else {
      this.doCreate();
    }
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  private doCreate(): void {
    const v = this.form.getRawValue();
    const request: CreateBudgetItemRequest = {
      month: v.month,
      budgetFlow: v.budgetFlow as 'Income' | 'Expense',
      plannedAmount: v.plannedAmount,
      plannedCurrency: 'ZAR',
      payerSplit: v.payerSplit as PayerAllocationDto[],
      attributionSplit: v.attributionSplit as unknown as AttributionAllocationDto[],
    };
    this.budgetItemApi
      .createItem(this.budgetId(), this.categoryId(), request)
      .subscribe((result) => {
        this.saving.set(false);
        if (isSuccess(result)) {
          this.saved.emit(result.value);
        } else {
          this.handleError(result.error);
        }
      });
  }

  private doUpdate(existing: BudgetItemDto): void {
    const request = this.buildUpdateRequest(existing);
    this.budgetItemApi
      .updateItem(this.budgetId(), this.categoryId(), existing.id, request)
      .subscribe((result) => {
        this.saving.set(false);
        if (isSuccess(result)) {
          this.saved.emit(result.value);
        } else {
          this.handleError(result.error);
        }
      });
  }

  private buildUpdateRequest(existing: BudgetItemDto): UpdateBudgetItemRequest {
    const v = this.form.getRawValue();
    const request: UpdateBudgetItemRequest = {};

    if (v.plannedAmount !== existing.plannedAmount) {
      request.plannedAmount = v.plannedAmount;
      request.plannedCurrency = 'ZAR';
    }
    if (v.realizedAmount !== existing.realizedAmount) {
      request.realizedAmount = v.realizedAmount ?? undefined;
      request.realizedCurrency = v.realizedAmount != null ? 'ZAR' : undefined;
    }
    if (v.spentAmount !== existing.spentAmount) {
      request.spentAmount = v.spentAmount ?? undefined;
      request.spentCurrency = v.spentAmount != null ? 'ZAR' : undefined;
    }

    const payerSplit = v.payerSplit as PayerAllocationDto[];
    if (JSON.stringify(payerSplit) !== JSON.stringify(existing.payerSplit)) {
      request.payerSplit = payerSplit;
    }

    const attributionSplit = v.attributionSplit as unknown as AttributionAllocationDto[];
    if (JSON.stringify(attributionSplit) !== JSON.stringify(existing.attributionSplit)) {
      request.attributionSplit = attributionSplit;
    }

    return request;
  }

  private handleError(error: ApiError): void {
    if (hasValidationErrors(error)) {
      mapValidationErrorsToForm(error, this.form);
    } else {
      this.formError.set(getErrorMessage(error));
    }
  }

  private notifySplitChange(): void {
    this._splitChange.update((v) => v + 1);
  }
}
