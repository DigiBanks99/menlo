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

export type MnlInputType = 'text' | 'number' | 'email' | 'password' | 'search';
export type MnlInputValue = string | number | null;

const containerBaseClasses =
  'flex min-h-11 w-full items-center gap-3 rounded-lg border bg-mnl-surface px-3 shadow-sm transition-[border-color,box-shadow,background-color] duration-200 motion-reduce:transition-none';
const containerDefaultClasses =
  'border-mnl-border text-mnl-text focus-within:border-mnl-pink focus-within:ring-2 focus-within:ring-mnl-pink';
const containerErrorClasses =
  'border-mnl-red text-mnl-text ring-2 ring-mnl-red focus-within:border-mnl-red focus-within:ring-mnl-red';
const containerDisabledClasses =
  'pointer-events-none cursor-not-allowed bg-mnl-surface-alt text-mnl-subtext opacity-60 shadow-none';
const inputClasses =
  'form-input w-full min-w-0 border-0 bg-transparent px-0 py-0 text-sm text-inherit placeholder:text-mnl-subtext/80 shadow-none ring-0 outline-hidden focus:border-0 focus:ring-0 disabled:cursor-not-allowed disabled:text-mnl-subtext';

@Component({
  selector: 'mnl-input',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MnlInputComponent),
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
      data-testid="mnl-input-field"
    >
      <span
        aria-hidden="true"
        class="inline-flex shrink-0 items-center justify-center text-mnl-subtext empty:hidden"
      >
        <ng-content select="[mnlInputLeadingIcon]"></ng-content>
      </span>

      <input
        [attr.aria-invalid]="hasError() ? 'true' : null"
        [attr.autocomplete]="autocomplete() || null"
        [attr.id]="id() || null"
        [attr.name]="name() || null"
        [attr.placeholder]="placeholder() || null"
        [attr.type]="type()"
        [class]="controlClasses()"
        [disabled]="isDisabled()"
        [value]="displayValue()"
        [attr.data-testid]="testId()"
        (blur)="handleBlur()"
        (input)="handleInput($event)"
      />

      <span class="inline-flex shrink-0 items-center justify-center empty:hidden">
        <ng-content select="[mnlInputTrailingIcon]"></ng-content>
      </span>
    </label>
  `,
})
export class MnlInputComponent implements ControlValueAccessor {
  readonly autocomplete = input('');
  readonly disabled = input(false);
  readonly error = input<boolean | string | null>(null);
  readonly id = input('');
  readonly name = input('');
  readonly placeholder = input('');
  readonly testId = input('mnl-input');
  readonly type = input<MnlInputType>('text');

  readonly valueChange = output<MnlInputValue>();

  private readonly cvaDisabled = signal(false);
  private readonly currentValue = signal<MnlInputValue>('');
  private onChange: (value: MnlInputValue) => void = () => undefined;
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
  protected readonly controlClasses = computed(() =>
    [inputClasses, this.type() === 'number' ? 'text-right tabular-nums' : '']
      .filter(Boolean)
      .join(' '),
  );
  protected readonly displayValue = computed(() => {
    const value = this.currentValue();
    return value == null ? '' : `${value}`;
  });

  writeValue(value: MnlInputValue): void {
    this.currentValue.set(this.normalizeInputValue(value));
  }

  registerOnChange(fn: (value: MnlInputValue) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.cvaDisabled.set(isDisabled);
  }

  protected handleBlur(): void {
    this.onTouched();
  }

  protected handleInput(event: Event): void {
    if (this.isDisabled()) {
      return;
    }

    const nextValue = this.readValue(event);
    this.currentValue.set(nextValue);
    this.onChange(nextValue);
    this.valueChange.emit(nextValue);
  }

  private normalizeInputValue(value: MnlInputValue): MnlInputValue {
    if (this.type() !== 'number') {
      return value ?? '';
    }

    if (value == null || value === '') {
      return null;
    }

    const numericValue = typeof value === 'number' ? value : Number(value);
    return Number.isFinite(numericValue) ? numericValue : null;
  }

  private readValue(event: Event): MnlInputValue {
    const element = event.target as HTMLInputElement;

    if (this.type() !== 'number') {
      return element.value;
    }

    return element.value === '' || Number.isNaN(element.valueAsNumber)
      ? null
      : element.valueAsNumber;
  }
}
