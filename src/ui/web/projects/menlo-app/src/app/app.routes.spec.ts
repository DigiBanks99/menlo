import { describe, expect, it } from 'vitest';

import { authGuard } from './core/auth/auth.guard';
import { routes } from './app.routes';

describe('app routes', () => {
  it('should expose sign-in outside the guarded route tree', () => {
    const signInRoute = routes.find((route) => route.path === 'sign-in');

    expect(signInRoute?.loadComponent).toBeTypeOf('function');
  });

  it('should lazily resolve each routed component', async () => {
    const signInRoute = routes.find((route) => route.path === 'sign-in');
    const guardedRoot = routes.find((route) => route.path === '');
    const homeRoute = guardedRoot?.children?.find((child) => child.path === '');
    const budgetsRoute = guardedRoot?.children?.find((child) => child.path === 'budgets');
    const budgetDetailRoute = guardedRoot?.children?.find((child) => child.path === 'budgets/:id');
    const analyticsRoute = guardedRoot?.children?.find((child) => child.path === 'analytics');

    expect(await signInRoute?.loadComponent?.()).toBeTruthy();
    expect(await homeRoute?.loadComponent?.()).toBeTruthy();
    expect(await budgetsRoute?.loadComponent?.()).toBeTruthy();
    expect(await budgetDetailRoute?.loadComponent?.()).toBeTruthy();
    expect(await analyticsRoute?.loadComponent?.()).toBeTruthy();
  });

  it('should guard all application routes behind the auth guard', () => {
    const guardedRoot = routes.find((route) => route.path === '');

    expect(guardedRoot?.canActivateChild).toEqual([authGuard]);
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
