import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewChild,
  inject,
  input,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';

import { MnlTabBarComponent, type MnlTabBarItem } from '../../molecules/tab-bar';

@Component({
  selector: 'mnl-page-shell',
  standalone: true,
  imports: [MnlTabBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block min-h-dvh bg-mnl-bg text-mnl-text',
  },
  template: `
    <div class="flex min-h-dvh bg-mnl-bg text-mnl-text" data-testid="mnl-page-shell">
      <mnl-tab-bar [items]="items()" />

      <div class="flex min-h-dvh min-w-0 flex-1 flex-col">
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

  constructor() {
    this.router?.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.scrollContentToTop());
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
