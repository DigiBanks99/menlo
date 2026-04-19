import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it } from 'vitest';

import { BudgetApiService, BudgetResponse } from 'data-access-menlo-api';
import { success } from 'shared-util';
import { HomeComponent } from './home.component';

describe('HomeComponent', () => {
  const currentYear = new Date().getFullYear();
  const budget: BudgetResponse = {
    id: 'budget-1',
    year: currentYear,
    householdId: 'household-1',
    status: 'Active',
    categories: [],
    totalPlannedMonthlyAmount: { amount: 10000, currency: 'ZAR' },
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
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
    const navLinks = compiled.querySelectorAll('.nav-button');

    expect(compiled.querySelector('h1')?.textContent).toContain('Menlo Home Management');
    expect(navLinks).toHaveLength(2);
  });
});
