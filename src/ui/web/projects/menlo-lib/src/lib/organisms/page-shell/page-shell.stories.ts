import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { applicationConfig, type Meta, type StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { type MnlTabBarItem } from '../../molecules/tab-bar';
import { MnlPageShellComponent } from './page-shell.component';

const navigationItems: readonly MnlTabBarItem[] = [
  { icon: 'House', label: 'Home', route: '/' },
  { icon: 'Wallet', label: 'Budgets', route: '/budgets', badge: 4 },
  { icon: 'TrendingUp', label: 'Analytics', route: '/analytics' },
  { icon: 'Calendar', label: 'Planning', route: '/planning' },
];

const viewportOptions = {
  desktop1440: {
    name: 'Desktop 1440',
    styles: {
      height: '1024px',
      width: '1440px',
    },
    type: 'desktop',
  },
  mobile390: {
    name: 'Mobile 390',
    styles: {
      height: '844px',
      width: '390px',
    },
    type: 'mobile',
  },
} as const;

@Component({
  standalone: true,
  template: '',
})
class DummyStoryRouteComponent {}

@Component({
  selector: 'lib-page-shell-story-preview',
  standalone: true,
  imports: [MnlPageShellComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Organisms
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Page Shell</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            The page shell composes the responsive navigation scaffold with a padded, max-width
            content column that stays scrollable across route changes.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="overflow-hidden rounded-[2rem] shadow-sm ring-1 ring-mnl-border"
            >
              <mnl-page-shell [items]="items">
                <div class="space-y-6">
                  <header class="space-y-3">
                    <span
                      class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                    >
                      {{ theme.label }}
                    </span>
                    <h2 class="m-0 text-3xl font-bold tracking-tight">Household dashboard</h2>
                    <p class="m-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
                      Page content remains centered, padded, and scrollable while navigation adapts
                      to the current viewport.
                    </p>
                  </header>

                  <div class="grid gap-4 xl:grid-cols-3">
                    <article
                      class="rounded-2xl bg-mnl-surface p-5 shadow-sm ring-1 ring-mnl-border"
                    >
                      <p
                        class="m-0 text-sm font-semibold uppercase tracking-[0.2em] text-mnl-subtext"
                      >
                        Planned
                      </p>
                      <p class="mt-3 mb-0 text-4xl font-bold tracking-tight">R 24 900</p>
                    </article>

                    <article
                      class="rounded-2xl bg-mnl-surface p-5 shadow-sm ring-1 ring-mnl-border"
                    >
                      <p
                        class="m-0 text-sm font-semibold uppercase tracking-[0.2em] text-mnl-subtext"
                      >
                        Spent
                      </p>
                      <p class="mt-3 mb-0 text-4xl font-bold tracking-tight">R 18 640</p>
                    </article>

                    <article
                      class="rounded-2xl bg-mnl-surface p-5 shadow-sm ring-1 ring-mnl-border"
                    >
                      <p
                        class="m-0 text-sm font-semibold uppercase tracking-[0.2em] text-mnl-subtext"
                      >
                        Remaining
                      </p>
                      <p class="mt-3 mb-0 text-4xl font-bold tracking-tight text-mnl-success">
                        R 6 260
                      </p>
                    </article>
                  </div>

                  @for (section of [1, 2, 3, 4]; track section) {
                    <article
                      class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border"
                    >
                      <h3 class="m-0 text-xl font-semibold">Section {{ section }}</h3>
                      <p class="mt-3 mb-0 text-sm leading-6 text-mnl-subtext">
                        This placeholder content makes the shell scrollable so viewport padding,
                        max-width constraints, and route-reset behavior are easy to review.
                      </p>
                    </article>
                  }
                </div>
              </mnl-page-shell>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class PageShellStoryPreviewComponent {
  protected readonly items = navigationItems;
  protected readonly themes = foundationThemes;

  private readonly router = inject(Router);

  constructor() {
    void this.router.navigateByUrl('/analytics');
  }
}

const meta: Meta<PageShellStoryPreviewComponent> = {
  title: 'Organisms/Page Shell',
  component: PageShellStoryPreviewComponent,
  decorators: [
    applicationConfig({
      providers: [
        provideRouter([
          { path: '', component: DummyStoryRouteComponent },
          { path: 'budgets', component: DummyStoryRouteComponent },
          { path: 'analytics', component: DummyStoryRouteComponent },
          { path: 'planning', component: DummyStoryRouteComponent },
        ]),
      ],
    }),
  ],
  parameters: {
    layout: 'fullscreen',
    viewport: {
      options: viewportOptions,
    },
  },
};

export default meta;

type Story = StoryObj<PageShellStoryPreviewComponent>;

export const Mobile: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'mobile390',
    },
  },
};

export const Desktop: Story = {
  parameters: {
    viewport: {
      defaultViewport: 'desktop1440',
    },
  },
};
