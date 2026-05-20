import {
  ChangeDetectionStrategy,
  Component,
  computed,
  forwardRef,
  input,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export type MnlAmountInputValue = number | null;

const containerBaseClasses =
  'flex min-h-11 w-full items-center gap-3 rounded-lg border bg-mnl-surface px-3 shadow-sm transition-[border-color,box-shadow,background-color] duration-200 motion-reduce:transition-none';
const containerDefaultClasses =
  'border-mnl-border text-mnl-text focus-within:border-mnl-pink focus-within:ring-2 focus-within:ring-mnl-pink';
const containerErrorClasses =
  'border-mnl-red text-mnl-text ring-2 ring-mnl-red focus-within:border-mnl-red focus-within:ring-mnl-red';
const containerDisabledClasses =
  'pointer-events-none cursor-not-allowed bg-mnl-surface-alt text-mnl-subtext opacity-60 shadow-none';
const inputClasses =
  'w-full min-w-0 border-0 bg-transparent px-0 py-0 text-right text-sm tabular-nums text-inherit placeholder:text-mnl-subtext/80 shadow-none ring-0 outline-hidden focus:border-0 focus:ring-0 disabled:cursor-not-allowed disabled:text-mnl-subtext';

@Component({
  selector: 'mnl-amount-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MnlAmountInputComponent),
      multi: true,
    },
  ],
  host: {
    class: 'block w-full',
  },
  template: `
    <label
      [attr.data-has-error]="hasError()"
      [class]="containerClasses()"
      data-testid="mnl-amount-input-field"
    >
      <span
        aria-hidden="true"
        class="inline-flex min-h-8 shrink-0 items-center rounded-md bg-mnl-surface-alt px-2.5 text-sm font-semibold text-mnl-subtext ring-1 ring-inset ring-mnl-border"
        data-testid="mnl-amount-input-prefix"
      >
        {{ currencyPrefix() }}
      </span>

      <input
        [attr.aria-invalid]="hasError() ? 'true' : null"
        [attr.id]="id() || null"
        [attr.name]="name() || null"
        [attr.placeholder]="placeholder() || null"
        [class]="controlClasses()"
        [disabled]="isDisabled()"
        [value]="displayValue()"
        data-testid="mnl-amount-input"
        inputmode="decimal"
        type="text"
        (blur)="handleBlur()"
        (focus)="handleFocus()"
        (input)="handleInput($event)"
      />
    </label>
  `,
})
export class MnlAmountInputComponent implements ControlValueAccessor {
  readonly currency = input('ZAR');
  readonly disabled = input(false);
  readonly error = input<boolean | string | null>(null);
  readonly id = input('');
  readonly name = input('');
  readonly placeholder = input('');

  readonly valueChange = output<MnlAmountInputValue>();

  private readonly cvaDisabled = signal(false);
  private readonly currentValue = signal<MnlAmountInputValue>(null);
  private readonly displayText = signal('');
  private readonly focused = signal(false);
  private onChange: (value: MnlAmountInputValue) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  protected readonly controlClasses = computed(() => inputClasses);
  protected readonly currencyPrefix = computed(() =>
    this.currency() === 'ZAR' ? 'R' : this.currency(),
  );
  protected readonly displayValue = computed(() => this.displayText());
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

  writeValue(value: MnlAmountInputValue): void {
    const normalizedValue = this.normalizeValue(value);
    this.currentValue.set(normalizedValue);
    this.displayText.set(
      this.focused() ? this.toEditableValue(normalizedValue) : this.formatAmount(normalizedValue),
    );
  }

  registerOnChange(fn: (value: MnlAmountInputValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.cvaDisabled.set(isDisabled);
  }

  protected handleBlur(): void {
    this.focused.set(false);
    this.displayText.set(this.formatAmount(this.currentValue()));
    this.onTouched();
  }

  protected handleFocus(): void {
    if (this.isDisabled()) {
      return;
    }

    this.focused.set(true);
    this.displayText.set(this.toEditableValue(this.currentValue()));
  }

  protected handleInput(event: Event): void {
    if (this.isDisabled()) {
      return;
    }

    const nextDisplayValue = (event.target as HTMLInputElement).value;
    const nextValue = this.parseValue(nextDisplayValue);

    this.displayText.set(nextDisplayValue);
    this.currentValue.set(nextValue);
    this.onChange(nextValue);
    this.valueChange.emit(nextValue);
  }

  private normalizeValue(value: MnlAmountInputValue): MnlAmountInputValue {
    if (value == null) {
      return null;
    }

    const numericValue = typeof value === 'number' ? value : Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private parseValue(value: string): MnlAmountInputValue {
    const normalizedValue = value.replace(/[^0-9.-]/g, '');
    if (
      normalizedValue === '' ||
      normalizedValue === '-' ||
      normalizedValue === '.' ||
      normalizedValue === '-.'
    ) {
      return null;
    }

    const numericValue = Number(normalizedValue);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private toEditableValue(value: MnlAmountInputValue): string {
    return value == null ? '' : `${value}`;
  }

  private formatAmount(value: MnlAmountInputValue): string {
    if (value == null) {
      return '';
    }

    return new Intl.NumberFormat('en-ZA', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
  }
}
