import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { CircleAlert, LucideAngularModule, Search } from 'lucide-angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlInputComponent } from './input.component';

@Component({
  selector: 'lib-input-story-preview',
  standalone: true,
  imports: [LucideAngularModule, MnlInputComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Input</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-input provides a rounded-lg field primitive with signal-based value changes, error
            styling, icon slots, and ControlValueAccessor support for Menlo forms.
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
                    Accent rings stay pink, error states stay red, and the field remains legible in
                    both themes.
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
                  Input types
                </h3>

                <div class="grid gap-3 md:grid-cols-2">
                  <mnl-input placeholder="Household name"></mnl-input>
                  <mnl-input placeholder="owner@menlo.home" type="email"></mnl-input>
                  <mnl-input placeholder="Secure password" type="password">
                    <lucide-icon
                      [img]="alertIcon"
                      class="size-4 text-mnl-subtext"
                      mnlInputTrailingIcon
                    ></lucide-icon>
                  </mnl-input>
                  <mnl-input placeholder="Search budgets" type="search">
                    <lucide-icon
                      [img]="searchIcon"
                      class="size-4 text-mnl-subtext"
                      mnlInputLeadingIcon
                    ></lucide-icon>
                  </mnl-input>
                  <mnl-input placeholder="Planned amount" type="number"></mnl-input>
                </div>
              </article>

              <article class="space-y-3 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  States
                </h3>

                <div class="space-y-3">
                  <mnl-input placeholder="Default state"></mnl-input>
                  <mnl-input error="Required field" placeholder="Error state"></mnl-input>
                  <mnl-input [disabled]="true" placeholder="Disabled state"></mnl-input>
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class InputStoryPreviewComponent {
  protected readonly themes = foundationThemes;
  protected readonly alertIcon = CircleAlert;
  protected readonly searchIcon = Search;
}

const meta: Meta<InputStoryPreviewComponent> = {
  title: 'Atoms/Input',
  component: InputStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<InputStoryPreviewComponent>;

export const Overview: Story = {};
