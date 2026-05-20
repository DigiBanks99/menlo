import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { ArrowUpRight, LucideAngularModule, Sparkles } from 'lucide-angular';

import { MnlBadgeComponent } from '../../atoms/badge';
import { foundationThemes } from '../../foundations/foundation-data';
import { MnlCardComponent } from '../card';
import { MnlStatComponent } from '../stat';
import { MnlPageHeaderComponent, mnlPageHeaderDefaultGradient } from './page-header.component';

const sunriseGradient =
  'linear-gradient(135deg, var(--mnl-color-warning) 0%, var(--mnl-color-accent) 55%, var(--mnl-color-gradient-end) 100%)';

@Component({
  selector: 'lib-page-header-story-preview',
  standalone: true,
  imports: [
    LucideAngularModule,
    MnlBadgeComponent,
    MnlCardComponent,
    MnlPageHeaderComponent,
    MnlStatComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Page Header</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-page-header gives Menlo screens a soft gradient hero while letting summary cards and
            key stats overlap the fold for a dashboard-style landing area.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="overflow-hidden rounded-2xl bg-mnl-bg shadow-sm ring-1 ring-mnl-border"
            >
              <mnl-page-header
                [gradient]="theme.mode === 'light' ? defaultGradient : alternateGradient"
              >
                <div class="space-y-4" mnlPageHeaderHero>
                  <mnl-badge size="sm" variant="neutral">
                    <lucide-icon
                      [img]="sparklesIcon"
                      class="size-3.5"
                      mnlBadgeLeading
                    ></lucide-icon>
                    {{ theme.label }} preview
                  </mnl-badge>

                  <div class="space-y-3">
                    <h2 class="m-0 text-3xl font-bold tracking-tight text-inherit">
                      Household snapshot
                    </h2>
                    <p class="m-0 max-w-2xl text-sm leading-6 text-black/70">
                      Use the hero band for headlines, context, and helpful meta while the summary
                      cards dip beneath the gradient edge.
                    </p>
                  </div>
                </div>

                <div class="grid gap-4 md:grid-cols-2">
                  <mnl-card>
                    <mnl-stat
                      label="Available to spend"
                      [trend]="{
                        direction: 'up',
                        value: '+6.2% vs last month',
                        variant: 'success',
                      }"
                      value="R 18 450"
                    ></mnl-stat>
                  </mnl-card>

                  <mnl-card>
                    <div class="flex items-start justify-between gap-4">
                      <div>
                        <p
                          class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext"
                        >
                          Next action
                        </p>
                        <h3 class="mt-2 mb-0 text-xl font-semibold text-mnl-text">
                          Review recurring payments
                        </h3>
                        <p class="mt-2 mb-0 text-sm leading-6 text-mnl-subtext">
                          A quick audit can free up room before the school-fees transfer lands.
                        </p>
                      </div>

                      <lucide-icon
                        [img]="arrowUpRightIcon"
                        class="mt-1 size-5 shrink-0 text-mnl-accent"
                      ></lucide-icon>
                    </div>
                  </mnl-card>
                </div>
              </mnl-page-header>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class PageHeaderStoryPreviewComponent {
  protected readonly alternateGradient = sunriseGradient;
  protected readonly arrowUpRightIcon = ArrowUpRight;
  protected readonly defaultGradient = mnlPageHeaderDefaultGradient;
  protected readonly sparklesIcon = Sparkles;
  protected readonly themes = foundationThemes;
}

const meta: Meta<PageHeaderStoryPreviewComponent> = {
  title: 'Molecules/Page Header',
  component: PageHeaderStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<PageHeaderStoryPreviewComponent>;

export const Overview: Story = {};
