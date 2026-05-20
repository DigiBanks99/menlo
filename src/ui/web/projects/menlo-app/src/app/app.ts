import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { MnlPageShellComponent, MnlTabBarItem } from 'menlo-lib';
import { filter, map, startWith } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MnlPageShellComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly router = inject(Router);
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  protected readonly navigationItems: readonly MnlTabBarItem[] = [
    { icon: 'House', label: 'Home', route: '/' },
    { icon: 'Wallet', label: 'Budgets', route: '/budgets' },
    { icon: 'TrendingUp', label: 'Analytics', route: '/analytics' },
  ];

  protected readonly showPageShell = computed(() => !this.currentUrl().startsWith('/sign-in'));
}
