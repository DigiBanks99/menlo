import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes, radiusExamples, shadowExamples } from './foundation-data';

@Component({
  selector: 'lib-foundations-shadows-radii-story',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Foundations
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Shadows & Radii</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            Menlo keeps elevation minimal and relies on generous rounding to make surfaces feel soft
            and touch-friendly across phones and desktop dashboards.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="rounded-2xl p-6 shadow-sm ring-1 ring-mnl-border"
            >
              <div class="space-y-6">
                <div class="flex items-center justify-between gap-4">
                  <div>
                    <h2 class="m-0 text-2xl font-semibold">{{ theme.label }}</h2>
                    <p class="mt-2 mb-0 text-sm text-mnl-subtext">
                      Elevation and rounding tokens previewed in {{ theme.mode }} mode.
                    </p>
                  </div>

                  <span
                    class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                  >
                    Soft surfaces, minimal depth
                  </span>
                </div>

                <div class="space-y-4">
                  <div>
                    <p class="m-0 text-sm font-semibold text-mnl-subtext">Shadow scale</p>
                    <div class="mt-3 grid gap-4 md:grid-cols-2">
                      @for (token of shadowExamples; track token.label) {
                        <article class="rounded-2xl bg-mnl-bg p-4 ring-1 ring-mnl-border">
                          <div class="min-h-28 p-5" [class]="token.classes">
                            <p class="m-0 text-base font-semibold">{{ token.label }}</p>
                            <p class="mt-2 mb-0 text-sm text-mnl-subtext">{{ token.note }}</p>
                          </div>
                          <code class="mt-3 block text-xs text-mnl-subtext">{{
                            token.classes
                          }}</code>
                        </article>
                      }
                    </div>
                  </div>

                  <div>
                    <p class="m-0 text-sm font-semibold text-mnl-subtext">Radius tokens</p>
                    <div class="mt-3 grid gap-4 md:grid-cols-3">
                      @for (token of radiusExamples; track token.label) {
                        <article class="rounded-2xl bg-mnl-bg p-4 ring-1 ring-mnl-border">
                          <div class="min-h-24 p-5" [class]="token.classes">
                            <p class="m-0 text-sm font-semibold">{{ token.label }}</p>
                          </div>
                          <p class="mt-3 mb-0 text-xs text-mnl-subtext">{{ token.note }}</p>
                          <code class="mt-2 block text-xs text-mnl-subtext">{{
                            token.classes
                          }}</code>
                        </article>
                      }
                    </div>
                  </div>
                </div>
              </div>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class FoundationsShadowsRadiiStoryComponent {
  protected readonly themes = foundationThemes;
  protected readonly shadowExamples = shadowExamples;
  protected readonly radiusExamples = radiusExamples;
}

const meta: Meta<FoundationsShadowsRadiiStoryComponent> = {
  title: 'Foundations/Shadows & Radii',
  component: FoundationsShadowsRadiiStoryComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<FoundationsShadowsRadiiStoryComponent>;

export const Overview: Story = {};
