import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes, spacingScale } from './foundation-data';

@Component({
  selector: 'lib-foundations-spacing-story',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Foundations
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Spacing</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            Menlo uses Tailwind's 4px spacing rhythm. These references make it easy to align cards,
            page gutters, and component internals around the same scale.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="rounded-2xl p-6 shadow-sm ring-1 ring-mnl-border"
            >
              <div class="flex items-center justify-between gap-4">
                <div>
                  <h2 class="m-0 text-2xl font-semibold">{{ theme.label }}</h2>
                  <p class="mt-2 mb-0 text-sm text-mnl-subtext">
                    Utilities from 1 (4px) through 16 (64px).
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  4px base unit
                </span>
              </div>

              <div class="mt-6 space-y-3">
                @for (space of spacingScale; track space.step) {
                  <article
                    class="grid items-center gap-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border md:grid-cols-[64px_minmax(0,1fr)_220px]"
                  >
                    <div>
                      <p class="m-0 text-sm font-semibold">Step {{ space.step }}</p>
                      <p class="mt-1 mb-0 text-xs text-mnl-subtext">{{ space.rem }}</p>
                    </div>

                    <div class="flex items-center gap-4">
                      <div
                        class="h-4 rounded-full bg-mnl-accent"
                        [style.width.px]="space.pixels"
                      ></div>
                      <p class="m-0 text-sm text-mnl-subtext">{{ space.pixels }}px</p>
                    </div>

                    <code class="text-xs text-mnl-subtext">{{ space.classNames }}</code>
                  </article>
                }
              </div>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class FoundationsSpacingStoryComponent {
  protected readonly themes = foundationThemes;
  protected readonly spacingScale = spacingScale;
}

const meta: Meta<FoundationsSpacingStoryComponent> = {
  title: 'Foundations/Spacing',
  component: FoundationsSpacingStoryComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<FoundationsSpacingStoryComponent>;

export const Overview: Story = {};
