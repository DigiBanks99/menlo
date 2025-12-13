import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'budgets',
    loadComponent: () => import('./budget/budget-list.component').then(m => m.BudgetListComponent)
  },
  {
    path: 'analytics',
    loadComponent: () => import('./budget/budget-analytics.component').then(m => m.BudgetAnalyticsComponent)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
