import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { IsActiveMatchOptions, NavigationEnd, Router, RouterLink } from '@angular/router';
import {
  Bell,
  Calendar,
  House,
  Landmark,
  ListTodo,
  LucideAngularModule,
  PiggyBank,
  Settings,
  TrendingUp,
  User,
  Wallet,
} from 'lucide-angular';
import { filter, map, startWith } from 'rxjs';

export interface MnlTabBarItem {
  readonly icon: string;
  readonly label: string;
  readonly route: string;
  readonly badge?: number;
}

const iconMap = {
  Bell,
  Calendar,
  House,
  Landmark,
  ListTodo,
  PiggyBank,
  Settings,
  TrendingUp,
  User,
  Wallet,
} as const;

const mobileLinkBaseClasses =
  'relative inline-flex min-h-14 min-w-0 flex-1 flex-col items-center justify-center gap-1 rounded-2xl px-3 py-2 text-xs font-semibold text-mnl-subtext transition-colors duration-200 hover:bg-mnl-surface-alt hover:text-mnl-text focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-mnl-accent focus-visible:ring-offset-2 focus-visible:ring-offset-mnl-bg motion-reduce:transition-none';
const mobileLinkActiveClasses =
  'bg-mnl-accent text-mnl-mocha-crust shadow-sm ring-1 ring-mnl-accent-strong/40';
const desktopLinkBaseClasses =
  'group inline-flex w-full items-center gap-3 rounded-2xl px-4 py-3 text-sm font-semibold text-mnl-subtext transition-colors duration-200 hover:bg-mnl-surface-alt hover:text-mnl-text focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-mnl-accent focus-visible:ring-offset-2 focus-visible:ring-offset-mnl-bg motion-reduce:transition-none';
const desktopLinkActiveClasses =
  'bg-mnl-accent text-mnl-mocha-crust shadow-sm ring-1 ring-mnl-accent-strong/40';

@Component({
  selector: 'mnl-tab-bar',
  standalone: true,
  imports: [LucideAngularModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'contents',
  },
  template: `
    <nav
      aria-label="Primary navigation"
      class="fixed inset-x-4 bottom-4 z-30 lg:hidden"
      data-layout="mobile"
      data-testid="mnl-tab-bar-mobile"
    >
      <div
        class="flex items-center gap-2 rounded-[1.75rem] border border-mnl-border/80 bg-mnl-surface/95 p-2 shadow-lg shadow-black/5 backdrop-blur supports-[backdrop-filter]:bg-mnl-surface/90"
      >
        @for (item of items(); track item.route) {
          <a
            [attr.aria-current]="isRouteActive(item.route) ? 'page' : null"
            [attr.data-active]="isRouteActive(item.route) ? 'true' : 'false'"
            [attr.data-route]="item.route"
            [attr.data-testid]="'mnl-tab-bar-mobile-item-' + item.route"
            [class]="mobileLinkClasses(item.route)"
            [routerLink]="item.route"
          >
            <span class="relative inline-flex">
              <lucide-icon
                [img]="resolveIcon(item.icon)"
                aria-hidden="true"
                class="size-5"
                data-testid="mnl-tab-bar-icon"
              ></lucide-icon>

              @if (item.badge != null) {
                <span
                  class="absolute -top-2 -right-2 inline-flex min-h-5 min-w-5 items-center justify-center rounded-full bg-mnl-error px-1 text-[0.65rem] font-bold leading-none text-mnl-mocha-crust"
                  data-testid="mnl-tab-bar-badge"
                >
                  {{ item.badge }}
                </span>
              }
            </span>

            <span class="truncate">{{ item.label }}</span>
          </a>
        }
      </div>
    </nav>

    <nav
      aria-label="Primary navigation"
      class="hidden h-dvh w-72 shrink-0 lg:flex"
      data-layout="desktop"
      data-testid="mnl-tab-bar-desktop"
    >
      <div class="flex w-full flex-col border-r border-mnl-border bg-mnl-surface/90 px-4 py-6">
        <div class="px-2">
          <p class="m-0 text-xs font-semibold uppercase tracking-[0.24em] text-mnl-accent">Menlo</p>
          <h2 class="mt-3 mb-0 text-2xl font-bold tracking-tight text-mnl-text">Household hub</h2>
          <p class="mt-2 mb-0 text-sm leading-6 text-mnl-subtext">
            Navigation adapts between a thumb-friendly tab bar and spacious desktop sidebar.
          </p>
        </div>

        <div class="mt-8 flex flex-1 flex-col gap-2">
          @for (item of items(); track item.route) {
            <a
              [attr.aria-current]="isRouteActive(item.route) ? 'page' : null"
              [attr.data-active]="isRouteActive(item.route) ? 'true' : 'false'"
              [attr.data-route]="item.route"
              [attr.data-testid]="'mnl-tab-bar-desktop-item-' + item.route"
              [class]="desktopLinkClasses(item.route)"
              [routerLink]="item.route"
            >
              <span
                class="inline-flex size-11 shrink-0 items-center justify-center rounded-2xl bg-mnl-surface-alt text-inherit ring-1 ring-mnl-border/70 transition-colors duration-200 motion-reduce:transition-none group-hover:bg-mnl-bg"
              >
                <lucide-icon
                  [img]="resolveIcon(item.icon)"
                  aria-hidden="true"
                  class="size-5"
                  data-testid="mnl-tab-bar-icon"
                ></lucide-icon>
              </span>

              <span class="min-w-0 flex-1 truncate">{{ item.label }}</span>

              @if (item.badge != null) {
                <span
                  class="inline-flex min-h-6 min-w-6 items-center justify-center rounded-full bg-mnl-error px-2 text-xs font-bold leading-none text-mnl-mocha-crust"
                  data-testid="mnl-tab-bar-badge"
                >
                  {{ item.badge }}
                </span>
              }
            </a>
          }
        </div>
      </div>
    </nav>
  `,
})
export class MnlTabBarComponent {
  readonly items = input.required<readonly MnlTabBarItem[]>();

  private readonly router = inject(Router);
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  private readonly exactRouteOptions: IsActiveMatchOptions = {
    paths: 'exact',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };

  private readonly nestedRouteOptions: IsActiveMatchOptions = {
    paths: 'subset',
    queryParams: 'ignored',
    fragment: 'ignored',
    matrixParams: 'ignored',
  };

  protected resolveIcon(icon: string) {
    return iconMap[icon as keyof typeof iconMap] ?? House;
  }

  protected desktopLinkClasses(route: string): string {
    return this.isRouteActive(route)
      ? `${desktopLinkBaseClasses} ${desktopLinkActiveClasses}`
      : desktopLinkBaseClasses;
  }

  protected isRouteActive(route: string): boolean {
    this.currentUrl();
    return this.router.isActive(route, this.routeMatchOptions(route));
  }

  protected mobileLinkClasses(route: string): string {
    return this.isRouteActive(route)
      ? `${mobileLinkBaseClasses} ${mobileLinkActiveClasses}`
      : mobileLinkBaseClasses;
  }

  protected routeMatchOptions(route: string): IsActiveMatchOptions {
    return route === '/' ? this.exactRouteOptions : this.nestedRouteOptions;
  }
}
