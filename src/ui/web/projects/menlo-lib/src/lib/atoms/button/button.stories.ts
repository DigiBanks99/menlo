import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { ArrowRight, Check, CircleAlert, LucideAngularModule } from 'lucide-angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlButtonComponent, MnlButtonSize, MnlButtonVariant } from './button.component';

const variants: readonly MnlButtonVariant[] = ['primary', 'secondary', 'ghost', 'destructive'];
const sizes: readonly MnlButtonSize[] = ['sm', 'md', 'lg'];

@Component({
  selector: 'lib-button-story-preview',
  standalone: true,
  imports: [LucideAngularModule, MnlButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Button</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-button is the shared action primitive for Menlo. These previews cover all variants,
            sizes, loading and disabled states, and projected Lucide icon examples in both themes.
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
                    Rounded-xl buttons with semantic Catppuccin tokens and accessible focus states.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <div class="space-y-4">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Variant × size matrix
                </h3>

                <div class="space-y-3">
                  @for (variant of variants; track variant) {
                    <div class="grid gap-3 sm:grid-cols-3">
                      @for (size of sizes; track size) {
                        <mnl-button [size]="size" [variant]="variant">
                          {{ variant }} {{ size }}
                        </mnl-button>
                      }
                    </div>
                  }
                </div>
              </div>

              <div class="grid gap-4 lg:grid-cols-2">
                <article class="space-y-3 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                  <h3
                    class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext"
                  >
                    States
                  </h3>
                  <div class="flex flex-wrap gap-3">
                    <mnl-button variant="primary">Enabled</mnl-button>
                    <mnl-button variant="secondary" [disabled]="true">Disabled</mnl-button>
                    <mnl-button variant="primary" [loading]="true">Saving</mnl-button>
                    <mnl-button variant="destructive">Delete</mnl-button>
                  </div>
                </article>

                <article class="space-y-3 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                  <h3
                    class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext"
                  >
                    Projected icons
                  </h3>
                  <div class="flex flex-wrap gap-3">
                    <mnl-button variant="primary">
                      <lucide-icon
                        [img]="checkIcon"
                        class="size-4"
                        mnlButtonLeadingIcon
                      ></lucide-icon>
                      Confirm
                    </mnl-button>
                    <mnl-button variant="ghost">
                      Next
                      <lucide-icon
                        [img]="arrowRightIcon"
                        class="size-4"
                        mnlButtonTrailingIcon
                      ></lucide-icon>
                    </mnl-button>
                    <mnl-button variant="destructive">
                      <lucide-icon
                        [img]="alertIcon"
                        class="size-4"
                        mnlButtonLeadingIcon
                      ></lucide-icon>
                      Delete
                    </mnl-button>
                  </div>
                </article>
              </div>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class ButtonStoryPreviewComponent {
  protected readonly themes = foundationThemes;
  protected readonly variants = variants;
  protected readonly sizes = sizes;
  protected readonly arrowRightIcon = ArrowRight;
  protected readonly checkIcon = Check;
  protected readonly alertIcon = CircleAlert;
}

const meta: Meta<ButtonStoryPreviewComponent> = {
  title: 'Atoms/Button',
  component: ButtonStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<ButtonStoryPreviewComponent>;

export const Overview: Story = {};
