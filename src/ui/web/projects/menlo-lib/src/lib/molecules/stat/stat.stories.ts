import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlCardComponent } from '../card';
import { MnlStatComponent, type MnlStatTrend } from './stat.component';

interface StatExample {
  readonly label: string;
  readonly value: string;
  readonly trend: MnlStatTrend | null;
}

const statExamples: readonly StatExample[] = [
  {
    label: 'Income landed',
    value: 'R 48 200',
    trend: { direction: 'up', value: '+5.8% vs last month', variant: 'success' },
  },
  {
    label: 'Overspend risk',
    value: 'R 3 260',
    trend: { direction: 'down', value: '-12% headroom', variant: 'error' },
  },
  {
    label: 'Emergency fund',
    value: 'R 18 900',
    trend: { direction: 'neutral', value: 'On plan', variant: 'neutral' },
  },
  {
    label: 'Savings transfer',
    value: 'R 6 400',
    trend: null,
  },
] as const;

@Component({
  selector: 'lib-stat-story-preview',
  standalone: true,
  imports: [MnlCardComponent, MnlStatComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Stat Display</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-stat turns key financial figures into readable summary blocks with optional
            directional trend badges.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="space-y-6 rounded-2xl p-6 shadow-sm ring-1 ring-mnl-border"
            >
              <div class="flex items-center justify-between gap-4">
                <div>
                  <h2 class="m-0 text-2xl font-semibold">{{ theme.label }}</h2>
                  <p class="mt-2 mb-0 text-sm text-mnl-subtext">
                    Large values stay prominent while the trend badges keep direction and status
                    scannable.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <mnl-card>
                <div class="grid gap-4 md:grid-cols-2">
                  @for (example of statExamples; track example.label) {
                    <div class="rounded-2xl bg-mnl-bg/50 p-4 ring-1 ring-mnl-border/50">
                      <mnl-stat
                        [label]="example.label"
                        [trend]="example.trend"
                        [value]="example.value"
                      />
                    </div>
                  }
                </div>
              </mnl-card>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class StatStoryPreviewComponent {
  protected readonly statExamples = statExamples;
  protected readonly themes = foundationThemes;
}

const meta: Meta<StatStoryPreviewComponent> = {
  title: 'Molecules/Stat Display',
  component: StatStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<StatStoryPreviewComponent>;

export const Overview: Story = {};
