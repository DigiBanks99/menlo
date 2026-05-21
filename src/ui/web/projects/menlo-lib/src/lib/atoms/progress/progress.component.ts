import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type MnlProgressVariant = 'accent' | 'success' | 'warning' | 'error';
export type MnlProgressLabelPosition = 'top' | 'inline';

const trackClasses = 'relative block h-2.5 w-full overflow-hidden rounded-[4px] bg-mnl-surface-alt';
const fillBaseClasses =
  'block h-full rounded-[4px] transition-[width,background-color] duration-500 ease-out motion-reduce:transition-none';

const variantClasses: Record<MnlProgressVariant, string> = {
  accent: 'bg-mnl-accent',
  success: 'bg-mnl-success',
  warning: 'bg-mnl-warning',
  error: 'bg-mnl-error',
};

@Component({
  selector: 'mnl-progress',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block w-full',
  },
  template: `
    @if (labelPosition() === 'inline' && label()) {
      <div class="flex items-center gap-3">
        <span class="shrink-0 text-sm font-semibold text-mnl-text">{{ label() }}</span>
        <div class="min-w-0 flex-1">
          <div
            [attr.aria-label]="accessibleLabel()"
            [attr.aria-valuemax]="100"
            [attr.aria-valuemin]="0"
            [attr.aria-valuenow]="normalizedValue()"
            [attr.data-variant]="variant()"
            class="block"
            data-testid="mnl-progress"
            role="progressbar"
          >
            <span [class]="trackClasses" aria-hidden="true">
              <span
                [class]="fillClasses()"
                [style.width.%]="normalizedValue()"
                aria-hidden="true"
                data-testid="mnl-progress-fill"
              ></span>
            </span>
          </div>
        </div>
      </div>
    } @else {
      <div class="space-y-2">
        @if (label()) {
          <span class="block text-sm font-semibold text-mnl-text">{{ label() }}</span>
        }

        <div
          [attr.aria-label]="accessibleLabel()"
          [attr.aria-valuemax]="100"
          [attr.aria-valuemin]="0"
          [attr.aria-valuenow]="normalizedValue()"
          [attr.data-variant]="variant()"
          class="block"
          data-testid="mnl-progress"
          role="progressbar"
        >
          <span [class]="trackClasses" aria-hidden="true">
            <span
              [class]="fillClasses()"
              [style.width.%]="normalizedValue()"
              aria-hidden="true"
              data-testid="mnl-progress-fill"
            ></span>
          </span>
        </div>
      </div>
    }
  `,
})
export class MnlProgressComponent {
  readonly ariaLabel = input('');
  readonly label = input('');
  readonly labelPosition = input<MnlProgressLabelPosition>('top');
  readonly value = input(0);
  readonly variant = input<MnlProgressVariant>('accent');

  protected readonly trackClasses = trackClasses;
  protected readonly accessibleLabel = computed(
    () => this.ariaLabel().trim() || this.label().trim() || 'Progress',
  );
  protected readonly fillClasses = computed(() =>
    [fillBaseClasses, variantClasses[this.variant()]].join(' '),
  );
  protected readonly normalizedValue = computed(() => clamp(this.value(), 0, 100));
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}
