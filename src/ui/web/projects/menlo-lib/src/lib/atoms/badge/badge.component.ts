import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type MnlBadgeVariant = 'success' | 'warning' | 'error' | 'info' | 'neutral';
export type MnlBadgeSize = 'sm' | 'md';

const baseClasses =
  'inline-flex max-w-fit items-center gap-1.5 rounded-full border font-semibold transition-colors duration-200 motion-reduce:transition-none';

const sizeClasses: Record<MnlBadgeSize, string> = {
  sm: 'min-h-5 px-2 py-0.5 text-xs',
  md: 'min-h-6 px-2.5 py-1 text-sm',
};

const variantClasses: Record<MnlBadgeVariant, string> = {
  success: 'border-transparent bg-mnl-success text-[#11111b]',
  warning: 'border-transparent bg-mnl-warning text-[#11111b]',
  error: 'border-transparent bg-mnl-error text-[#11111b]',
  info: 'border-transparent bg-mnl-info text-[#11111b]',
  neutral: 'border-mnl-border bg-mnl-surface-alt text-mnl-text',
};

@Component({
  selector: 'mnl-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'inline-flex align-middle',
  },
  template: `
    <span
      [attr.data-size]="size()"
      [attr.data-variant]="variant()"
      [class]="badgeClasses()"
      data-testid="mnl-badge"
    >
      @if (leadingDot()) {
        <span [class]="dotClasses()" aria-hidden="true" data-testid="mnl-badge-dot"></span>
      }

      <span
        aria-hidden="true"
        class="inline-flex shrink-0 items-center justify-center empty:hidden"
      >
        <ng-content select="[mnlBadgeLeading]"></ng-content>
      </span>

      <span class="inline-flex min-w-0 items-center justify-center">
        <ng-content></ng-content>
      </span>
    </span>
  `,
})
export class MnlBadgeComponent {
  readonly leadingDot = input(false);
  readonly size = input<MnlBadgeSize>('md');
  readonly variant = input<MnlBadgeVariant>('neutral');

  protected readonly badgeClasses = computed(() =>
    [baseClasses, sizeClasses[this.size()], variantClasses[this.variant()]].join(' '),
  );
  protected readonly dotClasses = computed(() =>
    [
      'inline-flex rounded-full bg-current opacity-70',
      this.size() === 'sm' ? 'size-1.5' : 'size-2',
    ].join(' '),
  );
}
