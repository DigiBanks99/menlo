import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { noReenterOnboardingGuard, onboardingGuard } from './core/onboarding/onboarding.guard';

export const routes: Routes = [
  {
    path: 'sign-in',
    loadComponent: () => import('./sign-in/sign-in.component').then((m) => m.SignInComponent),
  },
  {
    path: 'onboarding',
    canActivate: [authGuard, noReenterOnboardingGuard],
    loadComponent: () =>
      import('./pages/onboarding/onboarding.component').then((m) => m.OnboardingComponent),
  },
  {
    path: '',
    canActivateChild: [authGuard, onboardingGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./home/home.component').then((m) => m.HomeComponent),
      },
      {
        path: 'budgets',
        loadComponent: () =>
          import('./budget/budget-list.component').then((m) => m.BudgetListComponent),
      },
      {
        path: 'budgets/:id',
        loadComponent: () =>
          import('./budget/budget-detail.component').then((m) => m.BudgetDetailComponent),
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./budget/budget-analytics.component').then((m) => m.BudgetAnalyticsComponent),
      },
      {
        path: '**',
        redirectTo: '',
      },
    ],
  },
];
