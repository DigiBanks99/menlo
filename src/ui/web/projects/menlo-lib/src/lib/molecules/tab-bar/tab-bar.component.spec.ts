import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { House } from 'lucide-angular';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlTabBarComponent, MnlTabBarItem } from './tab-bar.component';

@Component({
  standalone: true,
  template: '',
})
class DummyRouteComponent {}

@Component({
  standalone: true,
  imports: [MnlTabBarComponent],
  template: ` <mnl-tab-bar [items]="items" /> `,
})
class TestHostComponent {
  readonly items: readonly MnlTabBarItem[] = [
    { icon: 'House', label: 'Home', route: '/' },
    { icon: 'Wallet', label: 'Budgets', route: '/budgets', badge: 3 },
    { icon: 'TrendingUp', label: 'Analytics', route: '/analytics' },
  ];
}

describe('MnlTabBarComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [
        provideRouter([
          { path: '', component: DummyRouteComponent },
          { path: 'budgets', component: DummyRouteComponent },
          { path: 'budgets/:id', component: DummyRouteComponent },
          { path: 'analytics', component: DummyRouteComponent },
        ]),
        provideZonelessChangeDetection(),
      ],
    }).compileComponents();
  });

  it('renders each navigation item in both mobile and desktop layouts', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;

    expect(host.querySelector('[data-testid="mnl-tab-bar-mobile"]')).toBeTruthy();
    expect(host.querySelector('[data-testid="mnl-tab-bar-desktop"]')).toBeTruthy();
    expect(host.querySelectorAll('[data-testid="mnl-tab-bar-icon"]')).toHaveLength(6);
    expect(
      host.querySelector('[data-testid="mnl-tab-bar-mobile-item-/budgets"]')?.textContent,
    ).toContain('Budgets');
    expect(
      host.querySelector('[data-testid="mnl-tab-bar-desktop-item-/analytics"]')?.textContent,
    ).toContain('Analytics');
  });

  it('marks nested routes as active for non-root items', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/budgets/42');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;

    expect(
      host
        .querySelector('[data-testid="mnl-tab-bar-mobile-item-/budgets"]')
        ?.getAttribute('data-active'),
    ).toBe('true');
    expect(
      host
        .querySelector('[data-testid="mnl-tab-bar-desktop-item-/budgets"]')
        ?.getAttribute('data-active'),
    ).toBe('true');
    expect(
      host.querySelector('[data-testid="mnl-tab-bar-desktop-item-/"]')?.getAttribute('data-active'),
    ).toBe('false');
  });

  it('renders optional badge counts on both layouts', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/budgets');
    fixture.detectChanges();

    const host = fixture.nativeElement as HTMLElement;
    const badges = Array.from(host.querySelectorAll('[data-testid="mnl-tab-bar-badge"]')).map(
      (badge) => badge.textContent?.trim(),
    );

    expect(badges).toEqual(['3', '3']);
  });

  it('falls back to the home icon when an unknown icon name is requested', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    const router = TestBed.inject(Router);

    await router.navigateByUrl('/');
    fixture.detectChanges();

    const component = fixture.debugElement.children[0].componentInstance as MnlTabBarComponent;

    expect(component['resolveIcon']('UnknownIcon')).toBe(House);
  });
});
