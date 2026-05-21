import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetApiService, BudgetResponse } from 'data-access-menlo-api';
import { success } from 'shared-util';
import { HomeComponent } from './home.component';

describe('HomeComponent', () => {
  const currentYear = new Date().getFullYear();
  let mockRouter: { navigateByUrl: ReturnType<typeof vi.fn> };
  const budget: BudgetResponse = {
    id: 'budget-1',
    year: currentYear,
    householdId: 'household-1',
    status: 'Active',
    categories: [],
    totalPlannedMonthlyAmount: { amount: 10000, currency: 'ZAR' },
  };

  beforeEach(async () => {
    mockRouter = { navigateByUrl: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: Router, useValue: mockRouter },
        {
          provide: BudgetApiService,
          useValue: {
            createOrEnsureBudget: () => of(success(budget)),
          },
        },
      ],
    }).compileComponents();
  });

  it('should render the home heading and primary navigation', () => {
    const fixture = TestBed.createComponent(HomeComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const actions = compiled.querySelectorAll(
      '[data-testid="home-primary-action"], [data-testid="home-secondary-action"]',
    );
    const featureCards = compiled.querySelectorAll('[data-testid="home-feature-card"]');

    expect(compiled.querySelector('[data-testid="mnl-page-header"]')).toBeTruthy();
    expect(compiled.querySelector('h1')?.textContent).toContain('Menlo Home Management');
    expect(actions).toHaveLength(2);
    expect(featureCards).toHaveLength(3);
    expect(compiled.querySelector('[data-testid="home-overview-placeholder"]')).toBeTruthy();
  });

  it('navigates to the selected route through the component helper', () => {
    const fixture = TestBed.createComponent(HomeComponent);
    fixture.detectChanges();

    (fixture.componentInstance as unknown as { navigateTo(path: '/analytics' | '/budgets'): void }).navigateTo(
      '/budgets',
    );

    expect(mockRouter.navigateByUrl).toHaveBeenCalledWith('/budgets');
  });
});
