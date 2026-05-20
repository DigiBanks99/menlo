import {
  AfterViewChecked,
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  forwardRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface MnlSelectOption {
  readonly value: string;
  readonly label: string;
  readonly disabled?: boolean;
}

export type MnlSelectValue = string | null;

const containerBaseClasses =
  'flex min-h-11 w-full items-center gap-3 rounded-lg border bg-mnl-surface px-3 shadow-sm transition-[border-color,box-shadow,background-color] duration-200 motion-reduce:transition-none';
const containerDefaultClasses =
  'border-mnl-border text-mnl-text focus-within:border-mnl-pink focus-within:ring-2 focus-within:ring-mnl-pink';
const containerErrorClasses =
  'border-mnl-red text-mnl-text ring-2 ring-mnl-red focus-within:border-mnl-red focus-within:ring-mnl-red';
const containerDisabledClasses =
  'pointer-events-none cursor-not-allowed bg-mnl-surface-alt text-mnl-subtext opacity-60 shadow-none';
const selectClasses =
  'form-select w-full min-w-0 appearance-none border-0 bg-transparent px-0 py-0 pr-7 text-sm text-inherit shadow-none ring-0 outline-hidden focus:border-0 focus:ring-0 disabled:cursor-not-allowed disabled:text-mnl-subtext';

@Component({
  selector: 'mnl-select',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MnlSelectComponent),
      multi: true,
    },
  ],
  host: {
    class: 'block w-full',
  },
  template: `
    <label
      [attr.aria-disabled]="isDisabled() ? 'true' : null"
      [attr.data-has-error]="hasError()"
      [class]="containerClasses()"
      data-testid="mnl-select-field"
    >
      <span
        aria-hidden="true"
        class="inline-flex shrink-0 items-center justify-center text-mnl-subtext empty:hidden"
      >
        <ng-content select="[mnlSelectLeadingIcon]"></ng-content>
      </span>

      <div class="relative min-w-0 flex-1">
        <select
          #selectElement
          [attr.aria-invalid]="hasError() ? 'true' : null"
          [attr.id]="id() || null"
          [attr.name]="name() || null"
          [class]="controlClasses()"
          [disabled]="isDisabled()"
          [value]="displayValue()"
          data-testid="mnl-select"
          (blur)="handleBlur()"
          (change)="handleChange($event)"
        >
          @if (placeholder()) {
            <option value="">{{ placeholder() }}</option>
          }

          @for (option of options(); track option.value) {
            <option [disabled]="option.disabled ?? false" [value]="option.value">
              {{ option.label }}
            </option>
          }

          <ng-content select="option,optgroup"></ng-content>
        </select>

        <span
          aria-hidden="true"
          class="pointer-events-none absolute inset-y-0 right-0 inline-flex items-center justify-center text-mnl-subtext"
        >
          <svg
            class="size-4"
            fill="none"
            stroke="currentColor"
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="1.75"
            viewBox="0 0 24 24"
          >
            <path d="m6 9 6 6 6-6" />
          </svg>
        </span>
      </div>
    </label>
  `,
})
export class MnlSelectComponent implements AfterViewChecked, ControlValueAccessor {
  readonly disabled = input(false);
  readonly error = input<boolean | string | null>(null);
  readonly id = input('');
  readonly name = input('');
  readonly options = input<readonly MnlSelectOption[]>([]);
  readonly placeholder = input('');

  readonly valueChange = output<MnlSelectValue>();

  private readonly selectElement = viewChild<ElementRef<HTMLSelectElement>>('selectElement');
  private readonly cvaDisabled = signal(false);
  private readonly currentValue = signal<MnlSelectValue>('');
  private onChange: (value: MnlSelectValue) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  protected readonly hasError = computed(() => Boolean(this.error()));
  protected readonly isDisabled = computed(() => this.disabled() || this.cvaDisabled());
  protected readonly containerClasses = computed(() =>
    [
      containerBaseClasses,
      this.hasError() ? containerErrorClasses : containerDefaultClasses,
      this.isDisabled() ? containerDisabledClasses : '',
    ]
      .filter(Boolean)
      .join(' '),
  );
  protected readonly controlClasses = computed(() => selectClasses);
  protected readonly displayValue = computed(() => this.currentValue() ?? '');

  writeValue(value: MnlSelectValue): void {
    this.currentValue.set(this.normalizeValue(value));
  }

  registerOnChange(fn: (value: MnlSelectValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.cvaDisabled.set(isDisabled);
  }

  ngAfterViewChecked(): void {
    const select = this.selectElement();
    if (select) {
      const value = this.displayValue();
      if (select.nativeElement.value !== value) {
        select.nativeElement.value = value;
      }
    }
  }

  protected handleBlur(): void {
    this.onTouched();
  }

  protected handleChange(event: Event): void {
    if (this.isDisabled()) {
      return;
    }

    const nextValue = this.readValue(event);
    this.currentValue.set(nextValue);
    this.onChange(nextValue);
    this.valueChange.emit(nextValue);
  }

  private normalizeValue(value: MnlSelectValue): MnlSelectValue {
    return value === '' || value == null ? '' : value;
  }

  private readValue(event: Event): MnlSelectValue {
    const element = event.target as HTMLSelectElement;
    return element.value === '' ? null : element.value;
  }
}
