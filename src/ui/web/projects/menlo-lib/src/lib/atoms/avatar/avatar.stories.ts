import { ChangeDetectionStrategy, Component } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlAvatarComponent, MnlAvatarSize } from './avatar.component';

interface AvatarExample {
  readonly fallback: string;
  readonly label: string;
  readonly size: MnlAvatarSize;
  readonly src?: string;
}

const sampleAvatarSvg =
  'data:image/svg+xml;utf8,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 viewBox=%220 0 56 56%22%3E%3Cdefs%3E%3ClinearGradient id=%22g%22 x1=%220%25%22 y1=%220%25%22 x2=%22100%25%22 y2=%22100%25%22%3E%3Cstop offset=%220%25%22 stop-color=%22%23ea76cb%22/%3E%3Cstop offset=%22100%25%22 stop-color=%22%237287fd%22/%3E%3C/linearGradient%3E%3C/defs%3E%3Crect width=%2256%22 height=%2256%22 rx=%2228%22 fill=%22url(%23g)%22/%3E%3Ctext x=%2250%25%22 y=%2253%25%22 font-family=%22Arial%22 font-size=%2222%22 fill=%22%2311111b%22 text-anchor=%22middle%22 dominant-baseline=%22middle%22%3EWB%3C/text%3E%3C/svg%3E';

const sizedAvatars: readonly AvatarExample[] = [
  { fallback: 'WB', label: 'Small', size: 'sm', src: sampleAvatarSvg },
  { fallback: 'WB', label: 'Medium', size: 'md', src: sampleAvatarSvg },
  { fallback: 'WB', label: 'Large', size: 'lg', src: sampleAvatarSvg },
];

@Component({
  selector: 'lib-avatar-story-preview',
  standalone: true,
  imports: [MnlAvatarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Avatar</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-avatar keeps profile imagery and initials consistent, with graceful fallback to
            initials or a default Lucide user icon when no image is available.
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
                    Circular portraits, initials, and icon fallbacks adapt to the current theme
                    without losing contrast.
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
                  Sizes
                </h3>

                <div class="flex flex-wrap items-end gap-4">
                  @for (avatar of sizedAvatars; track avatar.label) {
                    <div class="flex flex-col items-center gap-2">
                      <mnl-avatar
                        [alt]="'Wilco Boshoff'"
                        [fallback]="avatar.fallback"
                        [size]="avatar.size"
                        [src]="avatar.src || ''"
                      />
                      <span class="text-xs font-semibold text-mnl-subtext">{{ avatar.label }}</span>
                    </div>
                  }
                </div>
              </article>

              <article
                class="grid gap-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border md:grid-cols-3"
              >
                <div class="space-y-2">
                  <h3
                    class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext"
                  >
                    Image
                  </h3>
                  <mnl-avatar
                    [alt]="'Wilco Boshoff'"
                    fallback="WB"
                    size="lg"
                    [src]="sampleAvatarSvg"
                  />
                </div>

                <div class="space-y-2">
                  <h3
                    class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext"
                  >
                    Initials fallback
                  </h3>
                  <mnl-avatar [alt]="'Budget owner'" fallback="Budget Owner" size="lg" />
                </div>

                <div class="space-y-2">
                  <h3
                    class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext"
                  >
                    Icon fallback
                  </h3>
                  <mnl-avatar [alt]="'Unknown household member'" size="lg" />
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class AvatarStoryPreviewComponent {
  protected readonly sampleAvatarSvg = sampleAvatarSvg;
  protected readonly sizedAvatars = sizedAvatars;
  protected readonly themes = foundationThemes;
}

const meta: Meta<AvatarStoryPreviewComponent> = {
  title: 'Atoms/Avatar',
  component: AvatarStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<AvatarStoryPreviewComponent>;

export const Overview: Story = {};
