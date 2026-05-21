import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  forwardRef,
  input,
  output,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

const buttonBaseClasses =
  'inline-flex max-w-fit items-center gap-3 rounded-full px-1 py-1 text-sm font-semibold text-mnl-text transition-[opacity] duration-200 focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-mnl-pink focus-visible:ring-offset-2 focus-visible:ring-offset-mnl-bg motion-reduce:transition-none disabled:cursor-not-allowed disabled:opacity-60';
const trackBaseClasses =
  'relative inline-flex h-6 w-11 shrink-0 items-center rounded-full border transition-colors duration-200 motion-reduce:transition-none';
const trackOffClasses = 'border-mnl-border bg-mnl-surface-alt';
const trackOnClasses = 'border-mnl-accent-strong bg-mnl-accent';
const trackDisabledClasses = 'opacity-80';
const thumbBaseClasses =
  'inline-flex size-5 rounded-full bg-mnl-surface shadow-sm ring-1 ring-black/5 transition-transform duration-200 motion-reduce:transition-none';
const thumbOffClasses = 'translate-x-0';
const thumbOnClasses = 'translate-x-5';

@Component({
  selector: 'mnl-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => MnlToggleComponent),
      multi: true,
    },
  ],
  host: {
    class: 'inline-flex align-middle',
  },
  template: `
    <button
      [attr.aria-checked]="currentChecked() ? 'true' : 'false'"
      [attr.aria-disabled]="isDisabled() ? 'true' : null"
      [attr.data-state]="currentChecked() ? 'on' : 'off'"
      [attr.type]="'button'"
      [class]="buttonClasses()"
      [disabled]="isDisabled()"
      data-testid="mnl-toggle"
      role="switch"
      (blur)="handleBlur()"
      (click)="handleClick($event)"
      (keydown)="handleKeydown($event)"
    >
      <span [class]="trackClasses()" aria-hidden="true">
        <span [class]="thumbClasses()" data-testid="mnl-toggle-thumb"></span>
      </span>

      @if (label()) {
        <span class="min-w-0">{{ label() }}</span>
      }

      <span class="min-w-0 empty:hidden">
        <ng-content></ng-content>
      </span>
    </button>
  `,
})
export class MnlToggleComponent implements ControlValueAccessor {
  readonly checked = input<boolean | null>(null);
  readonly disabled = input(false);
  readonly label = input('');

  readonly checkedChange = output<boolean>();

  private readonly cvaDisabled = signal(false);
  protected readonly currentChecked = signal(false);
  private suppressNextClick = false;
  private onChange: (value: boolean) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  protected readonly isDisabled = computed(() => this.disabled() || this.cvaDisabled());
  protected readonly buttonClasses = computed(() => buttonBaseClasses);
  protected readonly trackClasses = computed(() =>
    [
      trackBaseClasses,
      this.currentChecked() ? trackOnClasses : trackOffClasses,
      this.isDisabled() ? trackDisabledClasses : '',
    ]
      .filter(Boolean)
      .join(' '),
  );
  protected readonly thumbClasses = computed(() =>
    [thumbBaseClasses, this.currentChecked() ? thumbOnClasses : thumbOffClasses].join(' '),
  );

  constructor() {
    effect(() => {
      const nextChecked = this.checked();
      if (nextChecked !== null) {
        this.currentChecked.set(nextChecked);
      }
    });
  }

  writeValue(value: boolean | null): void {
    this.currentChecked.set(Boolean(value));
  }

  registerOnChange(fn: (value: boolean) => void): void {
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

  protected handleClick(event: MouseEvent): void {
    if (this.isDisabled()) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    if (this.suppressNextClick) {
      this.suppressNextClick = false;
      return;
    }

    this.commitValue(!this.currentChecked());
  }

  protected handleKeydown(event: KeyboardEvent): void {
    if (!isToggleKey(event.key) || this.isDisabled()) {
      return;
    }

    event.preventDefault();
    this.suppressNextClick = true;
    this.commitValue(!this.currentChecked());
  }

  private commitValue(nextChecked: boolean): void {
    this.currentChecked.set(nextChecked);
    this.onChange(nextChecked);
    this.checkedChange.emit(nextChecked);
  }
}

function isToggleKey(key: string): boolean {
  return key === ' ' || key === 'Enter';
}
