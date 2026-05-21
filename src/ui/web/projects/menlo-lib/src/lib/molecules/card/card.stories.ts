import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { MnlButtonComponent } from '../../atoms/button';
import { MnlProgressComponent } from '../../atoms/progress';
import { foundationThemes } from '../../foundations/foundation-data';
import { MnlCardComponent, type MnlCardPadding } from './card.component';

interface CardExample {
  readonly description: string;
  readonly label: string;
  readonly padding: MnlCardPadding;
}

const paddingExamples: readonly CardExample[] = [
  {
    label: 'Small padding',
    padding: 'sm',
    description: 'Compact metadata cards and tight utility surfaces.',
  },
  {
    label: 'Medium padding',
    padding: 'md',
    description: 'Default spacing for dashboard cards and summary content.',
  },
  {
    label: 'Large padding',
    padding: 'lg',
    description: 'Comfortable layouts for richer card compositions.',
  },
] as const;

@Component({
  selector: 'lib-card-story-preview',
  standalone: true,
  imports: [MnlButtonComponent, MnlCardComponent, MnlProgressComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Card</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-card provides the reusable island container for summaries, lists, and action areas,
            with optional header/footer slots and an interactive hover treatment.
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
                    Cards keep their soft elevation and semantic content hierarchy across both Menlo
                    themes.
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
                  Padding variants
                </h3>

                <div class="grid gap-4 lg:grid-cols-3">
                  @for (example of paddingExamples; track example.label) {
                    <mnl-card [padding]="example.padding">
                      <div class="space-y-2">
                        <p class="m-0 text-sm font-semibold text-mnl-text">{{ example.label }}</p>
                        <p class="m-0 text-sm leading-6 text-mnl-subtext">
                          {{ example.description }}
                        </p>
                      </div>
                    </mnl-card>
                  }
                </div>
              </article>

              <article class="space-y-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Header, footer, and interactive surface
                </h3>

                <mnl-card [interactive]="true">
                  <div class="flex items-start justify-between gap-3" mnlCardHeader>
                    <div>
                      <p
                        class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-accent"
                      >
                        Monthly budget
                      </p>
                      <h4 class="mt-2 mb-0 text-lg font-semibold text-mnl-text">Household spend</h4>
                    </div>

                    <span class="text-xs font-medium text-mnl-subtext">Updated today</span>
                  </div>

                  <div class="space-y-4">
                    <p class="m-0 text-sm leading-6 text-mnl-subtext">
                      Track shared household commitments and keep an eye on utilization before the
                      next payday.
                    </p>

                    <mnl-progress
                      ariaLabel="Household spend utilization"
                      label="63% utilized"
                      labelPosition="inline"
                      [value]="63"
                      variant="warning"
                    />
                  </div>

                  <div class="flex flex-wrap gap-3" mnlCardFooter>
                    <mnl-button size="sm" variant="primary">Review budget</mnl-button>
                    <mnl-button size="sm" variant="secondary">Open details</mnl-button>
                  </div>
                </mnl-card>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class CardStoryPreviewComponent {
  protected readonly paddingExamples = paddingExamples;
  protected readonly themes = foundationThemes;
}

const meta: Meta<CardStoryPreviewComponent> = {
  title: 'Molecules/Card',
  component: CardStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<CardStoryPreviewComponent>;

export const Overview: Story = {};
