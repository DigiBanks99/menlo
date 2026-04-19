import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { BudgetListComponent } from './budget-list.component';

describe('BudgetListComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BudgetListComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should expose the seeded demo budgets', () => {
    const fixture = TestBed.createComponent(BudgetListComponent);

    expect(fixture.componentInstance.budgets()).toHaveLength(3);
    expect(fixture.componentInstance.budgets()[0]?.name).toBe('Monthly Household Budget');
  });
});
