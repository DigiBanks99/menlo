import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import {
  ArrowDownRight,
  ArrowUpRight,
  LucideAngularModule,
  Minus,
  type LucideIconData,
} from 'lucide-angular';

import { MnlBadgeComponent, type MnlBadgeVariant } from '../../atoms/badge';

export type MnlStatTrendDirection = 'up' | 'down' | 'neutral';
export type MnlStatTrendVariant = Extract<MnlBadgeVariant, 'success' | 'error' | 'neutral'>;

export interface MnlStatTrend {
  readonly direction: MnlStatTrendDirection;
  readonly value: string;
  readonly variant: MnlStatTrendVariant;
}

const directionIcons: Record<MnlStatTrendDirection, LucideIconData> = {
  up: ArrowUpRight,
  down: ArrowDownRight,
  neutral: Minus,
};

@Component({
  selector: 'mnl-stat',
  standalone: true,
  imports: [LucideAngularModule, MnlBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block',
  },
  template: `
    <article class="space-y-3" data-testid="mnl-stat">
      <p class="m-0 text-xs font-medium text-mnl-subtext" data-testid="mnl-stat-label">
        {{ label() }}
      </p>

      <p class="m-0 text-3xl font-bold tracking-tight text-mnl-text" data-testid="mnl-stat-value">
        {{ value() }}
      </p>

      @if (trend(); as trend) {
        <div [attr.data-direction]="trend.direction" data-testid="mnl-stat-trend">
          <mnl-badge [variant]="trend.variant" size="sm">
            <lucide-icon
              [attr.data-direction]="trend.direction"
              [img]="trendIcon(trend.direction)"
              class="size-3.5"
              data-testid="mnl-stat-trend-icon"
              mnlBadgeLeading
            ></lucide-icon>
            {{ trend.value }}
          </mnl-badge>
        </div>
      }
    </article>
  `,
})
export class MnlStatComponent {
  readonly label = input.required<string>();
  readonly trend = input<MnlStatTrend | null>(null);
  readonly value = input.required<string>();

  protected trendIcon(direction: MnlStatTrendDirection): LucideIconData {
    return directionIcons[direction];
  }
}
