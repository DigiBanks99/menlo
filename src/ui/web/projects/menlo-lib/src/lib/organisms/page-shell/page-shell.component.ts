import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewChild,
  computed,
  inject,
  input,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { LucideAngularModule, Moon, Sun } from 'lucide-angular';
import { filter } from 'rxjs';

import { MnlTabBarComponent, type MnlTabBarItem } from '../../molecules/tab-bar';
import { ThemeService } from '../../theme';

@Component({
  selector: 'mnl-page-shell',
  standalone: true,
  imports: [LucideAngularModule, MnlTabBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block min-h-dvh bg-mnl-bg text-mnl-text',
  },
  template: `
    <div class="flex min-h-dvh bg-mnl-bg text-mnl-text" data-testid="mnl-page-shell">
      <mnl-tab-bar [items]="items()" />

      <div class="flex min-h-dvh min-w-0 flex-1 flex-col">
        <header class="border-b border-mnl-border bg-mnl-surface/90 backdrop-blur supports-[backdrop-filter]:bg-mnl-surface/80">
          <div class="mx-auto flex w-full max-w-screen-2xl justify-end px-4 py-3 sm:px-6 lg:px-8">
            <button
              type="button"
              class="inline-flex size-10 items-center justify-center rounded-full border border-mnl-border bg-mnl-surface text-[--mnl-subtext] transition-colors duration-200 hover:bg-mnl-surface-alt hover:text-[--mnl-text] focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-mnl-accent focus-visible:ring-offset-2 focus-visible:ring-offset-mnl-bg motion-reduce:transition-none"
              data-testid="mnl-page-shell-theme-toggle"
              [attr.aria-label]="isDarkTheme() ? 'Switch to light mode' : 'Switch to dark mode'"
              (click)="toggleTheme()"
            >
              <lucide-icon
                [img]="isDarkTheme() ? sunIcon : moonIcon"
                aria-hidden="true"
                class="size-5"
              ></lucide-icon>
            </button>
          </div>
        </header>

        <div
          #contentViewport
          class="flex-1 overflow-y-auto pb-24 lg:pb-0"
          data-testid="mnl-page-shell-content"
        >
          <div
            class="mx-auto w-full max-w-screen-2xl px-4 py-6 sm:px-6 lg:px-8"
            data-testid="mnl-page-shell-container"
          >
            <ng-content></ng-content>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class MnlPageShellComponent {
  readonly items = input.required<readonly MnlTabBarItem[]>();

  @ViewChild('contentViewport', { static: true })
  private readonly contentViewport?: ElementRef<HTMLElement>;

  private readonly router = inject(Router, { optional: true });
  private readonly themeService = inject(ThemeService);

  protected readonly currentTheme = this.themeService.currentTheme;
  protected readonly isDarkTheme = computed(() => this.currentTheme() === 'dark');
  protected readonly moonIcon = Moon;
  protected readonly sunIcon = Sun;

  constructor() {
    this.router?.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.scrollContentToTop());
  }

  protected toggleTheme(): void {
    this.themeService.toggle();
  }

  private scrollContentToTop(): void {
    const viewport = this.contentViewport?.nativeElement;

    if (!viewport) {
      return;
    }

    if (typeof viewport.scrollTo === 'function') {
      viewport.scrollTo({ top: 0, left: 0, behavior: 'auto' });
      return;
    }

    viewport.scrollTop = 0;
  }
}
