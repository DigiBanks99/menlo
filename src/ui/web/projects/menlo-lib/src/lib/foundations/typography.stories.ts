import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes, typographyRoles } from './foundation-data';

@Component({
  selector: 'lib-foundations-typography-story',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Foundations
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Typography</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            Nunito Sans is the default typeface across the system. Each role below documents the
            intended Tailwind class recipe and usage guidance.
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
                    Typography roles shown in {{ theme.mode }} mode.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  Nunito Sans 400 / 500 / 600 / 700
                </span>
              </div>

              <div class="mt-6 space-y-4">
                @for (role of typographyRoles; track role.role) {
                  <article class="rounded-2xl bg-mnl-surface p-5 ring-1 ring-mnl-border">
                    <div class="flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <p class="m-0 text-sm font-semibold text-mnl-subtext">{{ role.role }}</p>
                        <p class="mt-1 mb-0 text-xs text-mnl-subtext">{{ role.classes }}</p>
                      </div>
                      <p class="m-0 text-xs text-mnl-subtext">{{ role.note }}</p>
                    </div>

                    <div class="mt-4" [class]="role.classes">{{ role.sample }}</div>
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
class FoundationsTypographyStoryComponent {
  protected readonly themes = foundationThemes;
  protected readonly typographyRoles = typographyRoles;
}

const meta: Meta<FoundationsTypographyStoryComponent> = {
  title: 'Foundations/Typography',
  component: FoundationsTypographyStoryComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<FoundationsTypographyStoryComponent>;

export const Overview: Story = {};
