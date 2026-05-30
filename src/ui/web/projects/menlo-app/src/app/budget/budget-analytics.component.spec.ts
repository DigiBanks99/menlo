import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { BudgetAnalyticsComponent } from './budget-analytics.component';

describe('BudgetAnalyticsComponent', () => {
  let component: BudgetAnalyticsComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BudgetAnalyticsComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    const fixture = TestBed.createComponent(BudgetAnalyticsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should expose seeded metrics, categories, and insights', () => {
    expect(component.summaryMetrics()).toHaveLength(4);
    expect(component.categories()).toHaveLength(6);
    expect(component.aiInsights()).toHaveLength(3);
  });

  it('should expose period options and default selected period', () => {
    const c = component as unknown as { selectedPeriod: () => string; periodOptions: unknown[] };
    expect(c.periodOptions).toHaveLength(4);
    expect(c.selectedPeriod()).toBe('current');
  });

  it('should compute the selectedPeriodLabel from the selected period', () => {
    const c = component as unknown as {
      selectedPeriodLabel: () => string;
      updateSelectedPeriod: (p: string | null) => void;
    };
    expect(c.selectedPeriodLabel()).toBe('Current Month');
    c.updateSelectedPeriod('last');
    expect(c.selectedPeriodLabel()).toBe('Last Month');
    c.updateSelectedPeriod('quarter');
    expect(c.selectedPeriodLabel()).toBe('This Quarter');
    c.updateSelectedPeriod('year');
    expect(c.selectedPeriodLabel()).toBe('This Year');
  });

  describe('categoryProgressVariantFor', () => {
    it('returns "error" when percentage is >= 95', () => {
      const c = component as unknown as { categoryProgressVariantFor: (p: number) => string };
      expect(c.categoryProgressVariantFor(95)).toBe('error');
      expect(c.categoryProgressVariantFor(100)).toBe('error');
    });

    it('returns "warning" when percentage is >= 85 but < 95', () => {
      const c = component as unknown as { categoryProgressVariantFor: (p: number) => string };
      expect(c.categoryProgressVariantFor(85)).toBe('warning');
      expect(c.categoryProgressVariantFor(90)).toBe('warning');
    });

    it('returns "success" when percentage is < 85', () => {
      const c = component as unknown as { categoryProgressVariantFor: (p: number) => string };
      expect(c.categoryProgressVariantFor(84)).toBe('success');
      expect(c.categoryProgressVariantFor(0)).toBe('success');
    });
  });

  describe('insightLabelFor', () => {
    it('returns "Warning" for warning insights', () => {
      const c = component as unknown as { insightLabelFor: (t: string) => string };
      expect(c.insightLabelFor('warning')).toBe('Warning');
    });

    it('returns "Success" for success insights', () => {
      const c = component as unknown as { insightLabelFor: (t: string) => string };
      expect(c.insightLabelFor('success')).toBe('Success');
    });

    it('returns "Tip" for tip insights', () => {
      const c = component as unknown as { insightLabelFor: (t: string) => string };
      expect(c.insightLabelFor('tip')).toBe('Tip');
    });
  });

  describe('insightVariantFor', () => {
    it('returns "warning" for warning insights', () => {
      const c = component as unknown as { insightVariantFor: (t: string) => string };
      expect(c.insightVariantFor('warning')).toBe('warning');
    });

    it('returns "success" for success insights', () => {
      const c = component as unknown as { insightVariantFor: (t: string) => string };
      expect(c.insightVariantFor('success')).toBe('success');
    });

    it('returns "info" for tip insights', () => {
      const c = component as unknown as { insightVariantFor: (t: string) => string };
      expect(c.insightVariantFor('tip')).toBe('info');
    });
  });

  describe('metricBadgeVariantFor', () => {
    it('returns "warning" for warning metrics', () => {
      const c = component as unknown as { metricBadgeVariantFor: (t: string) => string };
      expect(c.metricBadgeVariantFor('warning')).toBe('warning');
    });

    it('returns "success" for success metrics', () => {
      const c = component as unknown as { metricBadgeVariantFor: (t: string) => string };
      expect(c.metricBadgeVariantFor('success')).toBe('success');
    });

    it('returns "info" for primary metrics', () => {
      const c = component as unknown as { metricBadgeVariantFor: (t: string) => string };
      expect(c.metricBadgeVariantFor('primary')).toBe('info');
    });
  });

  describe('metricTrendFor', () => {
    const base = { id: '1', title: 'T', value: 'V', icon: '🎯', type: 'primary' as const };

    it('returns a downward error trend for negative changeType', () => {
      const c = component as unknown as {
        metricTrendFor: (m: unknown) => { direction: string; variant: string; value: string };
      };
      const result = c.metricTrendFor({ ...base, change: '-10%', changeType: 'negative' });
      expect(result.direction).toBe('down');
      expect(result.variant).toBe('error');
      expect(result.value).toBe('-10%');
    });

    it('returns a neutral trend for neutral changeType', () => {
      const c = component as unknown as {
        metricTrendFor: (m: unknown) => { direction: string; variant: string };
      };
      const result = c.metricTrendFor({ ...base, change: '0%', changeType: 'neutral' });
      expect(result.direction).toBe('neutral');
      expect(result.variant).toBe('neutral');
    });

    it('returns an upward success trend for positive changeType', () => {
      const c = component as unknown as {
        metricTrendFor: (m: unknown) => { direction: string; variant: string };
      };
      const result = c.metricTrendFor({ ...base, change: '+5%', changeType: 'positive' });
      expect(result.direction).toBe('up');
      expect(result.variant).toBe('success');
    });
  });

  describe('updateSelectedPeriod', () => {
    it('sets the period when a valid value is provided', () => {
      const c = component as unknown as {
        selectedPeriod: () => string;
        updateSelectedPeriod: (p: string | null) => void;
      };
      c.updateSelectedPeriod('last');
      expect(c.selectedPeriod()).toBe('last');
      c.updateSelectedPeriod('quarter');
      expect(c.selectedPeriod()).toBe('quarter');
      c.updateSelectedPeriod('year');
      expect(c.selectedPeriod()).toBe('year');
      c.updateSelectedPeriod('current');
      expect(c.selectedPeriod()).toBe('current');
    });

    it('falls back to "current" for null or invalid values', () => {
      const c = component as unknown as {
        selectedPeriod: () => string;
        updateSelectedPeriod: (p: string | null) => void;
      };
      c.updateSelectedPeriod(null);
      expect(c.selectedPeriod()).toBe('current');
      c.updateSelectedPeriod('unknown');
      expect(c.selectedPeriod()).toBe('current');
    });
  });
});
