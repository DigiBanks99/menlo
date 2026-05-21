import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  CategoryApiService,
  CategoryDto,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from 'data-access-menlo-api';
import {
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

const budgetFlowOptions: readonly MnlSelectOption[] = [
  { value: 'Income', label: 'Income' },
  { value: 'Expense', label: 'Expense' },
  { value: 'Both', label: 'Both' },
];

const attributionOptions: readonly MnlSelectOption[] = [
  { value: 'Main', label: 'Main' },
  { value: 'Rental', label: 'Rental' },
  { value: 'ServiceProvider', label: 'Service Provider' },
];

@Component({
  selector: 'app-category-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MnlButtonComponent,
    MnlFormFieldComponent,
    MnlFormLayoutComponent,
    MnlInputComponent,
    MnlSelectComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="block" data-testid="category-form">
      <mnl-form-layout>
        <div mnlFormTitle class="space-y-2">
          <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-mnl-accent">
            Category
          </p>
          <h3 class="m-0 text-2xl font-bold tracking-tight text-mnl-text">
            {{ isEditMode() ? 'Edit category' : 'Add category' }}
          </h3>
          <p class="m-0 text-sm leading-6 text-mnl-subtext">
            Define the budget flow, attribution, and ownership metadata for this category.
          </p>
        </div>

        <div class="space-y-6">
          <section class="grid gap-4 md:grid-cols-2">
            <mnl-form-field
              errorTestId="error-name"
              inputId="name"
              label="Name"
              [error]="nameErrorMessage()"
              [required]="true"
            >
              <mnl-input id="name" testId="input-name" formControlName="name" />
            </mnl-form-field>

            <mnl-form-field inputId="description" label="Description">
              <mnl-input
                id="description"
                testId="input-description"
                formControlName="description"
              />
            </mnl-form-field>
          </section>

          <section class="grid gap-4 md:grid-cols-2">
            <mnl-form-field
              errorTestId="error-budgetFlow"
              inputId="category-budgetFlow"
              label="Budget flow"
              [error]="budgetFlowErrorMessage()"
              [required]="true"
            >
              <mnl-select
                id="category-budgetFlow"
                placeholder="-- Select --"
                testId="select-budgetFlow"
                [options]="budgetFlowOptions"
                formControlName="budgetFlow"
              />
            </mnl-form-field>

            <mnl-form-field inputId="attribution" label="Attribution">
              <mnl-select
                id="attribution"
                placeholder="-- None --"
                testId="select-attribution"
                [options]="attributionOptions"
                formControlName="attribution"
              />
            </mnl-form-field>
          </section>

          <section class="grid gap-4 md:grid-cols-2">
            <mnl-form-field inputId="incomeContributor" label="Income contributor">
              <mnl-input
                id="incomeContributor"
                testId="input-incomeContributor"
                formControlName="incomeContributor"
              />
            </mnl-form-field>

            <mnl-form-field inputId="responsiblePayer" label="Responsible payer">
              <mnl-input
                id="responsiblePayer"
                testId="input-responsiblePayer"
                formControlName="responsiblePayer"
              />
            </mnl-form-field>
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

          <mnl-button type="submit" [loading]="saving()" testId="btn-save">
            {{ saving() ? 'Saving...' : isEditMode() ? 'Update' : 'Create' }}
          </mnl-button>
        </div>
      </mnl-form-layout>
    </form>
  `,
})
export class CategoryFormComponent {
  private readonly categoryApi = inject(CategoryApiService);
  private readonly toastService = inject(MnlToastService);

  readonly budgetId = input.required<string>();
  readonly category = input<CategoryDto | null>(null);
  readonly parentId = input<string | null>(null);

  readonly saved = output<CategoryDto>();
  readonly cancelled = output<void>();

  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);

  readonly isEditMode = computed(() => !!this.category());
  protected readonly attributionOptions = attributionOptions;
  protected readonly budgetFlowOptions = budgetFlowOptions;

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
    const category = this.category();
    if (!category) {
      return;
    }

    this.form.patchValue({
      name: category.name,
      description: category.description ?? '',
      budgetFlow: category.budgetFlow,
      attribution: category.attribution ?? '',
      incomeContributor: category.incomeContributor ?? '',
      responsiblePayer: category.responsiblePayer ?? '',
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.formError.set(null);

    const category = this.category();
    if (category) {
      this.doUpdate(category.id);
      return;
    }

    this.doCreate();
  }

  onCancel(): void {
    this.cancelled.emit();
  }

  protected budgetFlowErrorMessage(): string | null {
    return this.controlErrorMessage(this.form.controls.budgetFlow, 'Budget flow is required');
  }

  protected nameErrorMessage(): string | null {
    return this.controlErrorMessage(this.form.controls.name, 'Name is required');
  }

  private controlErrorMessage(control: AbstractControl, requiredMessage: string): string | null {
    if (!control.touched || !control.errors) {
      return null;
    }

    if (typeof control.errors['api'] === 'string') {
      return control.errors['api'] as string;
    }

    if (control.errors['required']) {
      return requiredMessage;
    }

    return 'Invalid value';
  }

  private doCreate(): void {
    const request = this.buildCreateRequest();
    this.categoryApi.createCategory(this.budgetId(), request).subscribe((result) => {
      this.saving.set(false);
      if (isSuccess(result)) {
        this.toastService.show('Category created.', { variant: 'success' });
        this.saved.emit(result.value);
      } else {
        this.handleError(result.error);
      }
    });
  }

  private doUpdate(categoryId: string): void {
    const request = this.buildUpdateRequest();
    this.categoryApi.updateCategory(this.budgetId(), categoryId, request).subscribe((result) => {
      this.saving.set(false);
      if (isSuccess(result)) {
        this.toastService.show('Category updated.', { variant: 'success' });
        this.saved.emit(result.value);
      } else {
        this.handleError(result.error);
      }
    });
  }

  private buildCreateRequest(): CreateCategoryRequest {
    const value = this.form.getRawValue();
    const request: CreateCategoryRequest = {
      name: value.name,
      budgetFlow: value.budgetFlow as 'Income' | 'Expense' | 'Both',
    };

    if (this.parentId()) {
      request.parentId = this.parentId()!;
    }

    if (value.description) {
      request.description = value.description;
    }

    if (value.attribution) {
      request.attribution = value.attribution as 'Main' | 'Rental' | 'ServiceProvider';
    }

    if (value.incomeContributor) {
      request.incomeContributor = value.incomeContributor;
    }

    if (value.responsiblePayer) {
      request.responsiblePayer = value.responsiblePayer;
    }

    return request;
  }

  private buildUpdateRequest(): UpdateCategoryRequest {
    const value = this.form.getRawValue();
    const request: UpdateCategoryRequest = {
      name: value.name,
      budgetFlow: value.budgetFlow as 'Income' | 'Expense' | 'Both',
    };

    if (value.description) {
      request.description = value.description;
    }

    if (value.attribution) {
      request.attribution = value.attribution as 'Main' | 'Rental' | 'ServiceProvider';
    }

    if (value.incomeContributor) {
      request.incomeContributor = value.incomeContributor;
    }

    if (value.responsiblePayer) {
      request.responsiblePayer = value.responsiblePayer;
    }

    return request;
  }

  private handleError(error: ApiError): void {
    const message = getErrorMessage(error);
    if (error.kind === 'problem' && error.status === 409) {
      const nameControl = this.form.get('name')!;
      nameControl.setErrors({ api: message });
      nameControl.markAsTouched();
      this.toastService.show(message, { variant: 'warning' });
      return;
    }

    if (hasValidationErrors(error)) {
      mapValidationErrorsToForm(error, this.form);
      this.toastService.show('Please fix the highlighted validation errors.', {
        variant: 'warning',
      });
      return;
    }

    this.formError.set(message);
    this.toastService.show(message, { variant: 'error' });
  }
}
