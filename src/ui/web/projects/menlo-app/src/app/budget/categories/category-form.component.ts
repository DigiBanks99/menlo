import { Component, computed, inject, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  CategoryApiService,
  CategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from 'data-access-menlo-api';
import {
  ApiError,
  getErrorMessage,
  hasValidationErrors,
  isSuccess,
  mapValidationErrorsToForm,
} from 'shared-util';

@Component({
  selector: 'app-category-form',
  imports: [ReactiveFormsModule],
  template: `
    <form
      [formGroup]="form"
      (ngSubmit)="onSubmit()"
      class="category-form"
      data-testid="category-form"
    >
      <div class="form-field">
        <label for="name">Name *</label>
        <input id="name" formControlName="name" data-testid="input-name" />
        @if (form.controls.name.touched && form.controls.name.errors) {
          <span class="field-error" data-testid="error-name">
            @if (form.controls.name.errors['required']) {
              Name is required
            } @else if (form.controls.name.errors['api']) {
              {{ form.controls.name.errors['api'] }}
            }
          </span>
        }
      </div>

      <div class="form-field">
        <label for="description">Description</label>
        <input id="description" formControlName="description" data-testid="input-description" />
      </div>

      <div class="form-field">
        <label for="budgetFlow">Budget Flow *</label>
        <select id="budgetFlow" formControlName="budgetFlow" data-testid="select-budgetFlow">
          <option value="">-- Select --</option>
          <option value="Income">Income</option>
          <option value="Expense">Expense</option>
          <option value="Both">Both</option>
        </select>
        @if (form.controls.budgetFlow.touched && form.controls.budgetFlow.errors) {
          <span class="field-error" data-testid="error-budgetFlow">Budget flow is required</span>
        }
      </div>

      <div class="form-field">
        <label for="attribution">Attribution</label>
        <select id="attribution" formControlName="attribution" data-testid="select-attribution">
          <option value="">-- None --</option>
          <option value="Main">Main</option>
          <option value="Rental">Rental</option>
          <option value="ServiceProvider">ServiceProvider</option>
        </select>
      </div>

      <div class="form-field">
        <label for="incomeContributor">Income Contributor</label>
        <input
          id="incomeContributor"
          formControlName="incomeContributor"
          data-testid="input-incomeContributor"
        />
      </div>

      <div class="form-field">
        <label for="responsiblePayer">Responsible Payer</label>
        <input
          id="responsiblePayer"
          formControlName="responsiblePayer"
          data-testid="input-responsiblePayer"
        />
      </div>

      @if (formError()) {
        <div class="error-banner" data-testid="form-error">{{ formError() }}</div>
      }

      <div class="form-actions">
        <button type="submit" class="btn-primary" [disabled]="saving()" data-testid="btn-save">
          {{ saving() ? 'Saving...' : isEditMode() ? 'Update' : 'Create' }}
        </button>
        <button type="button" class="btn-secondary" (click)="onCancel()" data-testid="btn-cancel">
          Cancel
        </button>
      </div>
    </form>
  `,
  styles: [
    `
      .category-form {
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

      .form-field input,
      .form-field select {
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
export class CategoryFormComponent {
  private readonly categoryApi = inject(CategoryApiService);

  readonly budgetId = input.required<string>();
  readonly category = input<CategoryDto | null>(null);
  readonly parentId = input<string | null>(null);

  readonly saved = output<CategoryDto>();
  readonly cancelled = output<void>();

  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);

  readonly isEditMode = computed(() => !!this.category());

  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    budgetFlow: new FormControl<'Income' | 'Expense' | 'Both' | ''>('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    attribution: new FormControl<'Main' | 'Rental' | 'ServiceProvider' | ''>('', {
      nonNullable: true,
    }),
    incomeContributor: new FormControl('', { nonNullable: true }),
    responsiblePayer: new FormControl('', { nonNullable: true }),
  });

  ngOnInit(): void {
    const cat = this.category();
    if (cat) {
      this.form.patchValue({
        name: cat.name,
        description: cat.description ?? '',
        budgetFlow: cat.budgetFlow,
        attribution: cat.attribution ?? '',
        incomeContributor: cat.incomeContributor ?? '',
        responsiblePayer: cat.responsiblePayer ?? '',
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    const cat = this.category();
    if (cat) {
      this.doUpdate(cat.id);
    } else {
      this.doCreate();
    }
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  private doCreate(): void {
    const request: CreateCategoryRequest = this.buildCreateRequest();
    this.categoryApi.createCategory(this.budgetId(), request).subscribe((result) => {
      this.saving.set(false);
      if (isSuccess(result)) {
        this.saved.emit(result.value);
      } else {
        this.handleError(result.error);
      }
    });
  }

  private doUpdate(categoryId: string): void {
    const request: UpdateCategoryRequest = this.buildUpdateRequest();
    this.categoryApi.updateCategory(this.budgetId(), categoryId, request).subscribe((result) => {
      this.saving.set(false);
      if (isSuccess(result)) {
        this.saved.emit(result.value);
      } else {
        this.handleError(result.error);
      }
    });
  }

  private buildCreateRequest(): CreateCategoryRequest {
    const v = this.form.getRawValue();
    const request: CreateCategoryRequest = {
      name: v.name,
      budgetFlow: v.budgetFlow as 'Income' | 'Expense' | 'Both',
    };
    if (this.parentId()) request.parentId = this.parentId()!;
    if (v.description) request.description = v.description;
    if (v.attribution) request.attribution = v.attribution as 'Main' | 'Rental' | 'ServiceProvider';
    if (v.incomeContributor) request.incomeContributor = v.incomeContributor;
    if (v.responsiblePayer) request.responsiblePayer = v.responsiblePayer;
    return request;
  }

  private buildUpdateRequest(): UpdateCategoryRequest {
    const v = this.form.getRawValue();
    const request: UpdateCategoryRequest = {
      name: v.name,
      budgetFlow: v.budgetFlow as 'Income' | 'Expense' | 'Both',
    };
    if (v.description) request.description = v.description;
    if (v.attribution) request.attribution = v.attribution as 'Main' | 'Rental' | 'ServiceProvider';
    if (v.incomeContributor) request.incomeContributor = v.incomeContributor;
    if (v.responsiblePayer) request.responsiblePayer = v.responsiblePayer;
    return request;
  }

  private handleError(error: ApiError): void {
    if (error.kind === 'problem' && error.status === 409) {
      const nameControl = this.form.get('name');
      if (nameControl) {
        nameControl.setErrors({ api: getErrorMessage(error) });
        nameControl.markAsTouched();
      }
    } else if (hasValidationErrors(error)) {
      mapValidationErrorsToForm(error, this.form);
    } else {
      this.formError.set(getErrorMessage(error));
    }
  }
}
