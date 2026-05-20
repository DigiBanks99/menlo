import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlStatComponent, type MnlStatTrend } from './stat.component';

@Component({
  standalone: true,
  imports: [MnlStatComponent],
  template: ` <mnl-stat [label]="label" [trend]="trend" [value]="value" /> `,
})
class StatHostComponent {
  label = 'Available to spend';
  trend: MnlStatTrend | null = null;
  value = 'R 12 400';
}

describe('MnlStatComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders the label and value', () => {
    const fixture = TestBed.createComponent(StatHostComponent);
    fixture.detectChanges();

    expect(getLabel(fixture).textContent?.trim()).toBe('Available to spend');
    expect(getValue(fixture).textContent?.trim()).toBe('R 12 400');
  });

  it('renders the trend badge only when trend data is provided', () => {
    const withoutTrendFixture = TestBed.createComponent(StatHostComponent);
    withoutTrendFixture.detectChanges();

    expect(getTrend(withoutTrendFixture)).toBeNull();

    const withTrendFixture = TestBed.createComponent(StatHostComponent);
    withTrendFixture.componentInstance.trend = {
      direction: 'up',
      value: '+8.4% vs last month',
      variant: 'success',
    };
    withTrendFixture.detectChanges();

    expect(getTrend(withTrendFixture)?.textContent).toContain('+8.4% vs last month');
    expect(getBadge(withTrendFixture).dataset.variant).toBe('success');
  });

  it.each([
    [{ direction: 'up', value: '+12%', variant: 'success' }, 'up', 'success'],
    [{ direction: 'down', value: '-4%', variant: 'error' }, 'down', 'error'],
    [{ direction: 'neutral', value: 'No change', variant: 'neutral' }, 'neutral', 'neutral'],
  ] satisfies readonly [MnlStatTrend, string, string][])(
    'maps the %s trend direction and colour to the rendered badge',
    (trend, expectedDirection, expectedVariant) => {
      const fixture = TestBed.createComponent(StatHostComponent);
      fixture.componentInstance.trend = trend;
      fixture.detectChanges();

      expect(getTrend(fixture)?.dataset.direction).toBe(expectedDirection);
      expect(getTrendIcon(fixture)?.dataset.direction).toBe(expectedDirection);
      expect(getBadge(fixture).dataset.variant).toBe(expectedVariant);
    },
  );
});

function getBadge(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-badge"]') as HTMLElement;
}

function getLabel(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-stat-label"]') as HTMLElement;
}

function getTrend(fixture: { nativeElement: HTMLElement }): HTMLElement | null {
  return fixture.nativeElement.querySelector('[data-testid="mnl-stat-trend"]');
}

function getTrendIcon(fixture: { nativeElement: HTMLElement }): HTMLElement | null {
  return fixture.nativeElement.querySelector('[data-testid="mnl-stat-trend-icon"]');
}

function getValue(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-stat-value"]') as HTMLElement;
}
