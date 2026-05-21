import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { MnlBadgeComponent, MnlBadgeSize, MnlBadgeVariant } from './badge.component';

@Component({
  standalone: true,
  imports: [MnlBadgeComponent],
  template: `
    <mnl-badge [leadingDot]="leadingDot" [size]="size" [variant]="variant">
      <span mnlBadgeLeading>#</span>
      On track
    </mnl-badge>
  `,
})
class BadgeHostComponent {
  leadingDot = false;
  size: MnlBadgeSize = 'md';
  variant: MnlBadgeVariant = 'neutral';
}

describe('MnlBadgeComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BadgeHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  it.each([
    ['success', 'bg-mnl-success'],
    ['warning', 'bg-mnl-warning'],
    ['error', 'bg-mnl-error'],
    ['info', 'bg-mnl-info'],
    ['neutral', 'bg-mnl-surface-alt'],
  ] satisfies readonly [MnlBadgeVariant, string][])(
    'renders the %s variant with the expected token classes',
    (variant, expectedClass) => {
      const fixture = TestBed.createComponent(BadgeHostComponent);
      fixture.componentInstance.variant = variant;
      fixture.detectChanges();

      const badge = getBadge(fixture);

      expect(badge.dataset.variant).toBe(variant);
      expect(badge.className).toContain(expectedClass);
      expect(badge.textContent?.trim()).toContain('On track');
    },
  );

  it.each([['sm'], ['md']] satisfies readonly [MnlBadgeSize][])(
    'renders the %s size data attribute',
    (size) => {
      const fixture = TestBed.createComponent(BadgeHostComponent);
      fixture.componentInstance.size = size;
      fixture.detectChanges();

      expect(getBadge(fixture).dataset.size).toBe(size);
    },
  );

  it('renders an optional leading dot when requested', () => {
    const fixture = TestBed.createComponent(BadgeHostComponent);
    fixture.componentInstance.leadingDot = true;
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="mnl-badge-dot"]')).toBeTruthy();
  });

  it('does not render the leading dot when it is not requested', () => {
    const fixture = TestBed.createComponent(BadgeHostComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="mnl-badge-dot"]')).toBeNull();
  });

  it.each([
    ['sm', 'size-1.5'],
    ['md', 'size-2'],
  ] satisfies readonly [MnlBadgeSize, string][])(
    'applies the %s dot size class',
    (size, expectedClass) => {
      const fixture = TestBed.createComponent(BadgeHostComponent);
      fixture.componentInstance.leadingDot = true;
      fixture.componentInstance.size = size;
      fixture.detectChanges();

      const dot = fixture.nativeElement.querySelector('[data-testid="mnl-badge-dot"]') as HTMLElement;
      expect(dot.className).toContain(expectedClass);
    },
  );

  it('renders projected leading content for icons or markers', () => {
    const fixture = TestBed.createComponent(BadgeHostComponent);
    fixture.detectChanges();

    expect(getBadge(fixture).textContent).toContain('#');
  });
});

function getBadge(fixture: { nativeElement: HTMLElement }): HTMLSpanElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-badge"]') as HTMLSpanElement;
}
