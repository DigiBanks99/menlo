import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes, paletteTokenRows, semanticTokenExamples } from './foundation-data';

@Component({
  selector: 'lib-foundations-colours-story',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <div class="space-y-2">
            <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
              Foundations
            </p>
            <h1 class="m-0 text-3xl font-bold tracking-tight">Colours</h1>
            <p class="m-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
              Catppuccin tokens are documented side-by-side so designers and developers can compare
              Latte and Mocha values without switching context. Contrast ratios are measured against
              each theme's page background.
            </p>
          </div>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="rounded-2xl p-6 shadow-sm ring-1 ring-mnl-border"
            >
              <div class="flex items-start justify-between gap-4">
                <div>
                  <h2 class="m-0 text-2xl font-semibold">{{ theme.label }}</h2>
                  <p class="mt-2 mb-0 text-sm leading-6 text-mnl-subtext">
                    Semantic Menlo tokens inherit these values through CSS variables.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <div class="mt-6 grid gap-4 sm:grid-cols-2">
                @for (token of semanticTokenExamples; track token.label) {
                  <article class="space-y-3 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                    <div class="flex min-h-20 items-end rounded-xl p-3" [class]="token.classes">
                      <span class="text-sm font-semibold">{{ token.label }}</span>
                    </div>
                    <div>
                      <p class="m-0 text-sm font-semibold">{{ token.label }}</p>
                      <p class="mt-1 mb-0 text-xs text-mnl-subtext">{{ token.classes }}</p>
                      <p class="mt-2 mb-0 text-xs leading-5 text-mnl-subtext">{{ token.note }}</p>
                    </div>
                  </article>
                }
              </div>
            </section>
          }
        </div>

        <section
          class="overflow-hidden rounded-2xl bg-mnl-surface shadow-sm ring-1 ring-mnl-border"
        >
          <div class="border-b border-mnl-border px-6 py-4">
            <h2 class="m-0 text-xl font-semibold">Catppuccin palette tokens</h2>
          </div>

          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-mnl-border">
              <thead class="bg-mnl-surface-alt">
                <tr class="text-left text-sm font-semibold text-mnl-subtext">
                  <th class="px-6 py-4">Token</th>
                  <th class="px-6 py-4">Latte</th>
                  <th class="px-6 py-4">Contrast</th>
                  <th class="px-6 py-4">Mocha</th>
                  <th class="px-6 py-4">Contrast</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-mnl-border bg-mnl-surface text-sm">
                @for (row of paletteTokenRows; track row.token) {
                  <tr>
                    <td class="px-6 py-4 font-semibold">{{ row.token }}</td>
                    <td class="px-6 py-4">
                      <div class="flex items-center gap-3">
                        <span
                          class="size-8 rounded-lg ring-1 ring-black/10"
                          [style.backgroundColor]="row.latte"
                        ></span>
                        <code>{{ row.latte }}</code>
                      </div>
                    </td>
                    <td class="px-6 py-4 text-mnl-subtext">{{ row.latteContrast }}</td>
                    <td class="px-6 py-4">
                      <div class="flex items-center gap-3">
                        <span
                          class="size-8 rounded-lg ring-1 ring-black/10"
                          [style.backgroundColor]="row.mocha"
                        ></span>
                        <code>{{ row.mocha }}</code>
                      </div>
                    </td>
                    <td class="px-6 py-4 text-mnl-subtext">{{ row.mochaContrast }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </div>
  `,
})
class FoundationsColoursStoryComponent {
  protected readonly themes = foundationThemes;
  protected readonly semanticTokenExamples = semanticTokenExamples;
  protected readonly paletteTokenRows = paletteTokenRows;
}

const meta: Meta<FoundationsColoursStoryComponent> = {
  title: 'Foundations/Colours',
  component: FoundationsColoursStoryComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<FoundationsColoursStoryComponent>;

export const Overview: Story = {};
