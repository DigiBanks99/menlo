import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { CircleDollarSign, LucideAngularModule } from 'lucide-angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlSelectComponent, MnlSelectOption } from './select.component';

const selectOptions: readonly MnlSelectOption[] = [
  { value: 'income', label: 'Income' },
  { value: 'expense', label: 'Expense' },
  { value: 'transfer', label: 'Transfer' },
];

@Component({
  selector: 'lib-select-story-preview',
  standalone: true,
  imports: [LucideAngularModule, MnlSelectComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Select</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-select keeps Menlo dropdowns on the same rounded-lg visual language while supporting
            placeholder states, signal-driven options, and ControlValueAccessor wiring.
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
                    The native select stays accessible while the wrapper provides the design-system
                    surface and ring treatment.
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
                  <mnl-select [options]="options" placeholder="Choose a budget flow"></mnl-select>
                  <mnl-select
                    [options]="options"
                    error="Required field"
                    placeholder="Error state"
                  ></mnl-select>
                  <mnl-select
                    [disabled]="true"
                    [options]="options"
                    placeholder="Disabled state"
                  ></mnl-select>
                </div>
              </article>

              <article class="space-y-3 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  With icon slot
                </h3>

                <mnl-select [options]="options" placeholder="Assign a category">
                  <lucide-icon
                    [img]="currencyIcon"
                    class="size-4 text-mnl-subtext"
                    mnlSelectLeadingIcon
                  ></lucide-icon>
                </mnl-select>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class SelectStoryPreviewComponent {
  protected readonly currencyIcon = CircleDollarSign;
  protected readonly options = selectOptions;
  protected readonly themes = foundationThemes;
}

const meta: Meta<SelectStoryPreviewComponent> = {
  title: 'Atoms/Select',
  component: SelectStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<SelectStoryPreviewComponent>;

export const Overview: Story = {};
