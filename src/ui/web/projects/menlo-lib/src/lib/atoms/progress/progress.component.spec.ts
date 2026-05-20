import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlProgressComponent, MnlProgressVariant } from './progress.component';

@Component({
  standalone: true,
  imports: [MnlProgressComponent],
  template: `
    <mnl-progress
      [ariaLabel]="ariaLabel"
      [label]="label"
      [labelPosition]="labelPosition"
      [value]="value"
      [variant]="variant"
    />
  `,
})
class ProgressHostComponent {
  ariaLabel = '';
  label = 'Budget utilization';
  labelPosition: 'top' | 'inline' = 'top';
  value = 64;
  variant: MnlProgressVariant = 'success';
}

describe('MnlProgressComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProgressHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it('renders with progressbar semantics and a fill width that matches the input percentage', () => {
    const fixture = TestBed.createComponent(ProgressHostComponent);
    fixture.detectChanges();

    const progress = getProgress(fixture);
    const fill = getFill(fixture);

    expect(progress.getAttribute('role')).toBe('progressbar');
    expect(progress.getAttribute('aria-label')).toBe('Budget utilization');
    expect(progress.getAttribute('aria-valuemin')).toBe('0');
    expect(progress.getAttribute('aria-valuemax')).toBe('100');
    expect(progress.getAttribute('aria-valuenow')).toBe('64');
    expect(fill.style.width).toBe('64%');
  });

  it('clamps out-of-range values to the accepted 0-100 range', () => {
    const fixture = TestBed.createComponent(ProgressHostComponent);
    fixture.componentInstance.value = 140;
    fixture.detectChanges();

    const progress = getProgress(fixture);
    const fill = getFill(fixture);

    expect(progress.getAttribute('aria-valuenow')).toBe('100');
    expect(fill.style.width).toBe('100%');
  });

  it('supports the inline label layout while preserving the accessible name', () => {
    const fixture = TestBed.createComponent(ProgressHostComponent);
    fixture.componentInstance.labelPosition = 'inline';
    fixture.detectChanges();

    expect(getProgress(fixture).getAttribute('aria-label')).toBe('Budget utilization');
    expect(fixture.nativeElement.textContent).toContain('Budget utilization');
  });
});

function getFill(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-progress-fill"]') as HTMLElement;
}

function getProgress(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-progress"]') as HTMLElement;
}
