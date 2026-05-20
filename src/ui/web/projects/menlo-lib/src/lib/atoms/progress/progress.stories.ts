import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import {
  MnlProgressComponent,
  MnlProgressLabelPosition,
  MnlProgressVariant,
} from './progress.component';

interface ProgressExample {
  readonly label: string;
  readonly value: number;
  readonly variant: MnlProgressVariant;
}

const progressExamples: readonly ProgressExample[] = [
  { label: 'Emergency fund', value: 28, variant: 'accent' },
  { label: 'Groceries', value: 54, variant: 'success' },
  { label: 'School fees', value: 76, variant: 'warning' },
  { label: 'Utilities', value: 94, variant: 'error' },
];

@Component({
  selector: 'lib-progress-story-preview',
  standalone: true,
  imports: [MnlProgressComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Progress Bar</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-progress communicates budget health with semantic colour variants, accessible
            progressbar semantics, and reduced-motion friendly fill transitions.
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
                    Accent, success, warning, and error variants stay readable in both Menlo themes.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <article class="space-y-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Stacked labels
                </h3>

                <div class="space-y-4">
                  @for (example of progressExamples; track example.label) {
                    <mnl-progress
                      [label]="example.label"
                      [value]="example.value"
                      [variant]="example.variant"
                    />
                  }
                </div>
              </article>

              <article class="space-y-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Inline labels
                </h3>

                <div class="space-y-4">
                  @for (example of progressExamples; track example.label + '-inline') {
                    <mnl-progress
                      [label]="example.label"
                      [labelPosition]="inline"
                      [value]="example.value"
                      [variant]="example.variant"
                    />
                  }
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class ProgressStoryPreviewComponent {
  protected readonly inline: MnlProgressLabelPosition = 'inline';
  protected readonly progressExamples = progressExamples;
  protected readonly themes = foundationThemes;
}

const meta: Meta<ProgressStoryPreviewComponent> = {
  title: 'Atoms/Progress Bar',
  component: ProgressStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<ProgressStoryPreviewComponent>;

export const Overview: Story = {};
