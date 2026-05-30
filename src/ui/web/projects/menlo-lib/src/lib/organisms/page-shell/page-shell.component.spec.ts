import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { type Theme, ThemeService } from '../../theme';
import { MnlPageShellComponent } from './page-shell.component';

@Component({
  standalone: true,
  template: '',
})
class DummyRouteComponent {}

@Component({
  standalone: true,
  imports: [MnlPageShellComponent],
  template: `
    <mnl-page-shell [items]="items">
      <section data-testid="projected-content">Projected app content</section>
    </mnl-page-shell>
  `,
})
class TestHostComponent {
  readonly items = [
    { icon: 'House', label: 'Home', route: '/' },
    { icon: 'Wallet', label: 'Budgets', route: '/budgets' },
    { icon: 'TrendingUp', label: 'Analytics', route: '/analytics' },
  ] as const;
}

describe('MnlPageShellComponent', () => {
  const currentTheme = signal<Theme>('light');
  const toggleTheme = vi.fn();

  beforeEach(async () => {
    currentTheme.set('light');
    toggleTheme.mockReset();

    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [
        provideRouter([
          { path: '', component: DummyRouteComponent },
          { path: 'budgets', component: DummyRouteComponent },
          { path: 'analytics', component: DummyRouteComponent },
        ]),
        provideZonelessChangeDetection(),
        {
          provide: ThemeService,
          useValue: {
            currentTheme,
            toggle: toggleTheme,
          } satisfies Pick<ThemeService, 'currentTheme' | 'toggle'>,
        },
      ],
    }).compileComponents();
  });

  it('projects content into the scrollable page shell container', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const content = host.querySelector('[data-testid="mnl-page-shell-content"]');
    const container = host.querySelector('[data-testid="mnl-page-shell-container"]');

    expect(host.querySelector('[data-testid="projected-content"]')?.textContent).toContain(
      'Projected app content',
    );
    expect(content).toBeTruthy();
    expect(container?.className).toContain('px-4');
    expect(container?.className).toContain('lg:px-8');
    expect(container?.className).toContain('max-w-screen-2xl');
  });

  it('renders a theme toggle button and toggles the theme when clicked', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const button = host.querySelector(
      '[data-testid="mnl-page-shell-theme-toggle"]',
    ) as HTMLButtonElement | null;

    expect(button).toBeTruthy();
    expect(button?.getAttribute('aria-label')).toBe('Switch to dark mode');

    button?.click();

    expect(toggleTheme).toHaveBeenCalledTimes(1);

    currentTheme.set('dark');
    fixture.detectChanges();

    expect(button?.getAttribute('aria-label')).toBe('Switch to light mode');
  });

  it('scrolls the content viewport to the top after navigation completes', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/');
    fixture.detectChanges();

    const viewport = fixture.nativeElement.querySelector(
      '[data-testid="mnl-page-shell-content"]',
    ) as HTMLElement & { scrollTo: ReturnType<typeof vi.fn> };
    viewport.scrollTo = vi.fn();

    await router.navigateByUrl('/analytics');
    fixture.detectChanges();

    expect(viewport.scrollTo).toHaveBeenCalledWith({ top: 0, left: 0, behavior: 'auto' });
  });

  it('safely skips the scroll reset when the viewport is unavailable', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/');
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlPageShellComponent;
    component['contentViewport'] = undefined;

    expect(() => component['scrollContentToTop']()).not.toThrow();
  });
});
