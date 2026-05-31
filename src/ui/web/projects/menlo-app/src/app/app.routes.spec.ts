import { describe, expect, it } from 'vitest';

import { authGuard } from './core/auth/auth.guard';
import { noReenterOnboardingGuard, onboardingGuard } from './core/onboarding/onboarding.guard';
import { routes } from './app.routes';

describe('app routes', () => {
  it('should expose sign-in and onboarding outside the guarded route tree', () => {
    const signInRoute = routes.find((route) => route.path === 'sign-in');
    const onboardingRoute = routes.find((route) => route.path === 'onboarding');

    expect(signInRoute?.loadComponent).toBeTypeOf('function');
    expect(onboardingRoute?.loadComponent).toBeTypeOf('function');
    expect(onboardingRoute?.canActivate).toEqual([authGuard, noReenterOnboardingGuard]);
  });

  it('should lazily resolve each routed component', async () => {
    const signInRoute = routes.find((route) => route.path === 'sign-in');
    const onboardingRoute = routes.find((route) => route.path === 'onboarding');
    const guardedRoot = routes.find((route) => route.path === '');
    const homeRoute = guardedRoot?.children?.find((child) => child.path === '');
    const budgetsRoute = guardedRoot?.children?.find((child) => child.path === 'budgets');
    const budgetDetailRoute = guardedRoot?.children?.find((child) => child.path === 'budgets/:id');
    const analyticsRoute = guardedRoot?.children?.find((child) => child.path === 'analytics');

    const resolvedComponents = await Promise.all([
      signInRoute?.loadComponent?.(),
      onboardingRoute?.loadComponent?.(),
      homeRoute?.loadComponent?.(),
      budgetsRoute?.loadComponent?.(),
      budgetDetailRoute?.loadComponent?.(),
      analyticsRoute?.loadComponent?.(),
    ]);

    for (const resolvedComponent of resolvedComponents) {
      expect(resolvedComponent).toBeTruthy();
    }
  }, 15000);

  it('should guard all application routes behind auth and onboarding', () => {
    const guardedRoot = routes.find((route) => route.path === '');

    expect(guardedRoot?.canActivateChild).toEqual([authGuard, onboardingGuard]);
    expect(guardedRoot?.children?.map((child) => child.path)).toEqual([
      '',
      'budgets',
      'budgets/:id',
      'analytics',
      '**',
    ]);
  });

  it('should redirect the wildcard child route back to home', () => {
    const guardedRoot = routes.find((route) => route.path === '');
    const wildcardRoute = guardedRoot?.children?.find((child) => child.path === '**');

    expect(wildcardRoute?.redirectTo).toBe('');
  });
});
