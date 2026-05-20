import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { applicationConfig, type Meta, type StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlTabBarComponent, type MnlTabBarItem } from './tab-bar.component';

const navigationItems: readonly MnlTabBarItem[] = [
  { icon: 'House', label: 'Home', route: '/' },
  { icon: 'Wallet', label: 'Budgets', route: '/budgets', badge: 2 },
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
  selector: 'lib-tab-bar-story-preview',
  standalone: true,
  imports: [MnlTabBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-6xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">
            Molecules
          </p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Tab Bar</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            Resize the Storybook viewport to switch between the fixed mobile tab bar and the desktop
            sidebar while keeping the same router-aware navigation model.
          </p>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="relative min-h-[44rem] overflow-hidden rounded-[2rem] shadow-sm ring-1 ring-mnl-border"
            >
              <div class="absolute inset-0 bg-mnl-bg">
                <mnl-tab-bar [items]="items" />

                <div class="px-4 pt-6 pb-28 lg:pb-6 lg:pl-84 lg:pr-8">
                  <div class="mx-auto max-w-5xl space-y-4">
                    <span
                      class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                    >
                      {{ theme.label }}
                    </span>

                    <article
                      class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border"
                    >
                      <h2 class="m-0 text-2xl font-semibold">Budget overview</h2>
                      <p class="mt-2 mb-0 text-sm leading-6 text-mnl-subtext">
                        Active state, badges, and navigation positioning stay consistent across
                        viewports.
                      </p>
                    </article>

                    <div class="grid gap-4 md:grid-cols-2">
                      <article class="rounded-2xl bg-mnl-surface p-5 ring-1 ring-mnl-border">
                        <p
                          class="m-0 text-sm font-semibold uppercase tracking-[0.2em] text-mnl-subtext"
                        >
                          Due this week
                        </p>
                        <p class="mt-3 mb-0 text-3xl font-bold tracking-tight">2 categories</p>
                      </article>

                      <article class="rounded-2xl bg-mnl-surface p-5 ring-1 ring-mnl-border">
                        <p
                          class="m-0 text-sm font-semibold uppercase tracking-[0.2em] text-mnl-subtext"
                        >
                          Variance
                        </p>
                        <p class="mt-3 mb-0 text-3xl font-bold tracking-tight text-mnl-success">
                          +R 850
                        </p>
                      </article>
                    </div>
                  </div>
                </div>
              </div>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class TabBarStoryPreviewComponent {
  protected readonly items = navigationItems;
  protected readonly themes = foundationThemes;

  private readonly router = inject(Router);

  constructor() {
    void this.router.navigateByUrl('/budgets');
  }
}

const meta: Meta<TabBarStoryPreviewComponent> = {
  title: 'Molecules/Tab Bar',
  component: TabBarStoryPreviewComponent,
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

type Story = StoryObj<TabBarStoryPreviewComponent>;

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
