import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

export type MnlButtonVariant = 'primary' | 'secondary' | 'ghost' | 'destructive';
export type MnlButtonSize = 'sm' | 'md' | 'lg';
export type MnlButtonType = 'button' | 'submit' | 'reset';

const baseClasses =
  'inline-flex w-full items-center justify-center gap-2 whitespace-nowrap rounded-xl border text-sm font-semibold transition-colors duration-200 focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-offset-mnl-bg motion-reduce:transition-none disabled:cursor-not-allowed disabled:opacity-60 disabled:shadow-none';

const sizeClasses: Record<MnlButtonSize, string> = {
  sm: 'min-h-9 px-3 py-2 text-sm',
  md: 'min-h-11 px-4 py-2.5 text-sm',
  lg: 'min-h-12 px-5 py-3 text-base',
};

const variantClasses: Record<MnlButtonVariant, string> = {
  primary:
    'border-mnl-accent bg-mnl-accent text-mnl-mocha-crust shadow-sm hover:border-mnl-accent-strong hover:bg-mnl-accent-strong focus-visible:ring-mnl-accent',
  secondary:
    'border-mnl-border bg-mnl-surface text-mnl-text shadow-sm hover:bg-mnl-surface-alt focus-visible:ring-mnl-accent',
  ghost:
    'border-transparent bg-transparent text-mnl-text hover:bg-mnl-surface-alt/80 focus-visible:ring-mnl-accent',
  destructive:
    'border-mnl-error bg-mnl-error text-mnl-mocha-crust shadow-sm hover:opacity-90 focus-visible:ring-mnl-error',
};

@Component({
  selector: 'mnl-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'inline-flex align-middle',
  },
  template: `
    <button
      [attr.aria-busy]="loading() ? 'true' : null"
      [attr.aria-disabled]="isDisabled() ? 'true' : null"
      [attr.data-size]="size()"
      [attr.data-variant]="variant()"
      [attr.type]="type()"
      [class]="buttonClasses()"
      [disabled]="isDisabled()"
      [attr.data-testid]="testId()"
      (click)="handleClick($event)"
    >
      @if (loading()) {
        <span
          aria-hidden="true"
          class="inline-flex size-4 shrink-0 items-center justify-center"
          data-testid="mnl-button-spinner"
        >
          <span
            class="size-4 rounded-full border-2 border-current border-r-transparent animate-spin motion-reduce:animate-none"
          ></span>
        </span>
      }

      <span
        [class.hidden]="loading()"
        aria-hidden="true"
        class="inline-flex shrink-0 items-center justify-center empty:hidden"
      >
        <ng-content select="[mnlButtonLeadingIcon]"></ng-content>
      </span>

      <span class="inline-flex min-w-0 items-center justify-center">
        <ng-content></ng-content>
      </span>

      <span
        [class.hidden]="loading()"
        aria-hidden="true"
        class="inline-flex shrink-0 items-center justify-center empty:hidden"
      >
        <ng-content select="[mnlButtonTrailingIcon]"></ng-content>
      </span>
    </button>
  `,
})
export class MnlButtonComponent {
  readonly variant = input<MnlButtonVariant>('primary');
  readonly size = input<MnlButtonSize>('md');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly testId = input('mnl-button');
  readonly type = input<MnlButtonType>('button');

  readonly pressed = output<MouseEvent>();

  protected readonly isDisabled = computed(() => this.disabled() || this.loading());
  protected readonly buttonClasses = computed(() =>
    [baseClasses, sizeClasses[this.size()], variantClasses[this.variant()]].join(' '),
  );

  protected handleClick(event: MouseEvent): void {
    if (this.isDisabled()) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    this.pressed.emit(event);
  }
}
