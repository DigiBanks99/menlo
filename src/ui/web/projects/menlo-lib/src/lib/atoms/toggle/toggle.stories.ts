import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlToggleComponent } from './toggle.component';

@Component({
  selector: 'lib-toggle-story-preview',
  standalone: true,
  imports: [MnlToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Toggle</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-toggle provides an accessible switch primitive for Menlo settings and boolean
            controls with smooth thumb motion and form integration.
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
                    Keyboard-friendly switches that keep their motion subtle and readable in both
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
                  States
                </h3>

                <div class="flex flex-col items-start gap-4">
                  <mnl-toggle>Notifications</mnl-toggle>
                  <mnl-toggle [checked]="true">Budget alerts</mnl-toggle>
                  <mnl-toggle [checked]="true" [disabled]="true">Autopay enabled</mnl-toggle>
                  <mnl-toggle label="Projected label optional"></mnl-toggle>
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class ToggleStoryPreviewComponent {
  protected readonly themes = foundationThemes;
}

const meta: Meta<ToggleStoryPreviewComponent> = {
  title: 'Atoms/Toggle',
  component: ToggleStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<ToggleStoryPreviewComponent>;

export const Overview: Story = {};
