import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';
import { LucideAngularModule, MoonStar, Palette, Sun } from 'lucide-angular';

import { ThemeService } from './theme';

@Component({
  selector: 'lib-design-system-infrastructure-preview',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <section
        class="mx-auto max-w-2xl space-y-6 rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border"
      >
        <div class="flex items-start justify-between gap-4">
          <div class="space-y-2">
            <div class="inline-flex items-center gap-2 text-sm font-semibold text-mnl-accent">
              <lucide-icon [img]="paletteIcon" class="size-5"></lucide-icon>
              Tailwind + Theme infrastructure
            </div>
            <h2 class="m-0 text-3xl font-bold tracking-tight">Issue #321 foundation smoke test</h2>
            <p class="m-0 max-w-xl text-sm leading-6 text-mnl-subtext">
              This story verifies Catppuccin tokens, Nunito Sans, semantic theme colors, and the
              root ThemeService toggle inside Storybook.
            </p>
          </div>

          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-xl bg-mnl-accent px-4 py-2 font-semibold text-white shadow-sm transition-colors hover:bg-mnl-accent-strong focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-mnl-accent"
            (click)="toggleTheme()"
          >
            <lucide-icon [img]="isDarkTheme() ? sunIcon : moonIcon" class="size-4"></lucide-icon>
            {{ isDarkTheme() ? 'Switch to light' : 'Switch to dark' }}
          </button>
        </div>

        <div class="grid gap-4 md:grid-cols-3">
          <div class="rounded-xl bg-mnl-surface-alt p-4 ring-1 ring-mnl-border">
            <div class="text-xs font-medium uppercase tracking-wide text-mnl-subtext">Surface</div>
            <div class="mt-2 text-lg font-semibold">Rounded 16px cards</div>
            <div class="mt-1 text-sm text-mnl-subtext">shadow-sm / ring border tokens</div>
          </div>

          <div class="rounded-xl bg-mnl-surface-alt p-4 ring-1 ring-mnl-border">
            <div class="text-xs font-medium uppercase tracking-wide text-mnl-subtext">Theme</div>
            <div class="mt-2 text-lg font-semibold capitalize">{{ currentTheme() }}</div>
            <div class="mt-1 text-sm text-mnl-subtext">html.dark switches semantic tokens</div>
          </div>

          <div class="rounded-xl bg-mnl-surface-alt p-4 ring-1 ring-mnl-border">
            <div class="text-xs font-medium uppercase tracking-wide text-mnl-subtext">Font</div>
            <div class="mt-2 text-lg font-semibold">Nunito Sans 400–700</div>
            <div class="mt-1 text-sm text-mnl-subtext">Self-hosted via @fontsource</div>
          </div>
        </div>

        <div class="grid gap-3 sm:grid-cols-2">
          <div class="rounded-xl bg-mnl-latte-pink/15 p-4 text-sm ring-1 ring-mnl-latte-pink/20">
            Latte pink token available
          </div>
          <div
            class="rounded-xl bg-mnl-mocha-lavender/20 p-4 text-sm ring-1 ring-mnl-mocha-lavender/30"
          >
            Mocha lavender token available
          </div>
        </div>
      </section>
    </div>
  `,
})
class DesignSystemInfrastructurePreviewComponent {
  private readonly themeService = inject(ThemeService);

  protected readonly paletteIcon = Palette;
  protected readonly moonIcon = MoonStar;
  protected readonly sunIcon = Sun;
  protected readonly currentTheme = this.themeService.currentTheme;
  protected readonly isDarkTheme = computed(() => this.currentTheme() === 'dark');

  protected toggleTheme(): void {
    this.themeService.toggle();
  }
}

const meta: Meta<DesignSystemInfrastructurePreviewComponent> = {
  title: 'Foundations/Infrastructure',
  component: DesignSystemInfrastructurePreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<DesignSystemInfrastructurePreviewComponent>;

export const Playground: Story = {};
