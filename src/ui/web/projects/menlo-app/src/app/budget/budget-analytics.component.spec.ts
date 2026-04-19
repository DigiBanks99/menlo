import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { BudgetAnalyticsComponent } from './budget-analytics.component';

describe('BudgetAnalyticsComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BudgetAnalyticsComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('should expose seeded metrics, categories, and insights', () => {
    const fixture = TestBed.createComponent(BudgetAnalyticsComponent);

    expect(fixture.componentInstance.summaryMetrics()).toHaveLength(4);
    expect(fixture.componentInstance.categories()).toHaveLength(6);
    expect(fixture.componentInstance.aiInsights()).toHaveLength(3);
  });
});
