import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { CircleAlert, CircleDollarSign, LucideAngularModule } from 'lucide-angular';

import { MnlInputComponent } from '../../atoms/input';
import { MnlSelectComponent } from '../../atoms/select';
import { foundationThemes } from '../../foundations/foundation-data';
import { MnlFormFieldComponent } from './form-field.component';

@Component({
  selector: 'lib-form-field-story-preview',
  standalone: true,
  imports: [LucideAngularModule, MnlFormFieldComponent, MnlInputComponent, MnlSelectComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Form Field</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-form-field standardizes labels, hints, and validation messaging around projected
            inputs while keeping the atoms responsible for their own interaction behavior.
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
                    Labels stay compact, help text stays subtle, and validation stays visible in
                    both themes.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <article class="space-y-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <mnl-form-field
                  hint="Shown on the budget overview cards."
                  inputId="form-field-name"
                  label="Budget name"
                  [required]="true"
                >
                  <mnl-input id="form-field-name" placeholder="Groceries"></mnl-input>
                </mnl-form-field>

                <mnl-form-field
                  error="Choose the budget flow before saving."
                  inputId="form-field-flow"
                  label="Budget flow"
                  [required]="true"
                >
                  <mnl-select
                    id="form-field-flow"
                    [options]="flowOptions"
                    placeholder="Choose a flow"
                  >
                    <lucide-icon
                      [img]="currencyIcon"
                      class="size-4 text-mnl-subtext"
                      mnlSelectLeadingIcon
                    ></lucide-icon>
                  </mnl-select>
                </mnl-form-field>

                <mnl-form-field
                  hint="Optional context that appears below the control."
                  inputId="form-field-note"
                  label="Notes"
                >
                  <mnl-input id="form-field-note" placeholder="Income contributor details">
                    <lucide-icon
                      [img]="alertIcon"
                      class="size-4 text-mnl-subtext"
                      mnlInputTrailingIcon
                    ></lucide-icon>
                  </mnl-input>
                </mnl-form-field>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class FormFieldStoryPreviewComponent {
  protected readonly alertIcon = CircleAlert;
  protected readonly currencyIcon = CircleDollarSign;
  protected readonly flowOptions = [
    { value: 'Income', label: 'Income' },
    { value: 'Expense', label: 'Expense' },
  ] as const;
  protected readonly themes = foundationThemes;
}

const meta: Meta<FormFieldStoryPreviewComponent> = {
  title: 'Molecules/Form Field',
  component: FormFieldStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<FormFieldStoryPreviewComponent>;

export const Overview: Story = {};
