import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { Check, Clock3, LucideAngularModule } from 'lucide-angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlBadgeComponent, MnlBadgeSize, MnlBadgeVariant } from './badge.component';

const variants: readonly MnlBadgeVariant[] = ['success', 'warning', 'error', 'info', 'neutral'];
const sizes: readonly MnlBadgeSize[] = ['sm', 'md'];

@Component({
  selector: 'lib-badge-story-preview',
  standalone: true,
  imports: [LucideAngularModule, MnlBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Badge</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-badge packages status tokens into a compact pill that can show dot or icon
            affordances across both Menlo themes.
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
                    Semantic status pills keep their contrast while staying compact enough for
                    cards, lists, and dashboards.
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
                  Variant x size matrix
                </h3>

                <div class="space-y-3">
                  @for (variant of variants; track variant) {
                    <div class="flex flex-wrap gap-3">
                      @for (size of sizes; track size) {
                        <mnl-badge [size]="size" [variant]="variant">
                          {{ variant }} {{ size }}
                        </mnl-badge>
                      }
                    </div>
                  }
                </div>
              </article>

              <article class="space-y-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Leading affordances
                </h3>

                <div class="flex flex-wrap gap-3">
                  <mnl-badge [leadingDot]="true" variant="success">Synced</mnl-badge>
                  <mnl-badge variant="warning">
                    <lucide-icon [img]="clockIcon" class="size-3.5" mnlBadgeLeading></lucide-icon>
                    Review soon
                  </mnl-badge>
                  <mnl-badge variant="info">
                    <lucide-icon [img]="checkIcon" class="size-3.5" mnlBadgeLeading></lucide-icon>
                    Shared
                  </mnl-badge>
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class BadgeStoryPreviewComponent {
  protected readonly checkIcon = Check;
  protected readonly clockIcon = Clock3;
  protected readonly sizes = sizes;
  protected readonly themes = foundationThemes;
  protected readonly variants = variants;
}

const meta: Meta<BadgeStoryPreviewComponent> = {
  title: 'Atoms/Badge',
  component: BadgeStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<BadgeStoryPreviewComponent>;

export const Overview: Story = {};
