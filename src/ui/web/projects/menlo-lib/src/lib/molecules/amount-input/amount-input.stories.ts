import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlAmountInputComponent } from './amount-input.component';

@Component({
  selector: 'lib-amount-input-story-preview',
  standalone: true,
  imports: [MnlAmountInputComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Amount Input</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-amount-input adds a compound currency prefix, grouped number formatting, and
            ControlValueAccessor support for Menlo's budget forms.
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
                    The grouped value, accent focus ring, and error treatment stay consistent across
                    themes.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <article class="space-y-3 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  States
                </h3>

                <div class="space-y-3">
                  <mnl-amount-input placeholder="0.00"></mnl-amount-input>
                  <mnl-amount-input error="Planned amount is required" placeholder="Error state">
                  </mnl-amount-input>
                  <mnl-amount-input
                    currency="USD"
                    placeholder="Custom currency prefix"
                  ></mnl-amount-input>
                  <mnl-amount-input
                    [disabled]="true"
                    placeholder="Disabled amount"
                  ></mnl-amount-input>
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class AmountInputStoryPreviewComponent {
  protected readonly themes = foundationThemes;
}

const meta: Meta<AmountInputStoryPreviewComponent> = {
  title: 'Molecules/Amount Input',
  component: AmountInputStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<AmountInputStoryPreviewComponent>;

export const Overview: Story = {};
