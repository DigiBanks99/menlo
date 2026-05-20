import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlToastComponent } from './toast.component';
import type { MnlToastVariant } from './toast.types';

@Component({
  standalone: true,
  imports: [MnlToastComponent],
  template: `
    @if (showToast) {
      <mnl-toast
        [dismissible]="dismissible"
        [duration]="duration"
        [message]="message"
        [variant]="variant"
        (dismissed)="handleDismiss()"
      ></mnl-toast>
    }
  `,
})
class TestHostComponent {
  dismissible = true;
  duration = 4000;
  message = 'Toast message';
  showToast = true;
  variant: MnlToastVariant = 'info';
  readonly handleDismiss = vi.fn(() => {
    this.showToast = false;
  });
}

describe('MnlToastComponent', () => {
  let reducedMotion = false;

  beforeEach(async () => {
    reducedMotion = false;
    vi.useFakeTimers();

    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: vi.fn(() => createMediaQueryList(reducedMotion)),
    });

    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  afterEach(() => {
    vi.useRealTimers();
    reducedMotion = false;
  });

  it.each([
    ['success', 'text-mnl-success'],
    ['warning', 'text-mnl-warning'],
    ['error', 'text-mnl-error'],
    ['info', 'text-mnl-info'],
  ] satisfies [MnlToastVariant, string][])(
    'renders the %s variant with the correct semantic styling',
    async (variant, expectedClass) => {
      const fixture = TestBed.createComponent(TestHostComponent);
      fixture.componentInstance.variant = variant;
      fixture.detectChanges();
      await Promise.resolve();
      fixture.detectChanges();

      const toast = getToast(fixture);
      const icon = fixture.nativeElement.querySelector(
        '[data-testid="mnl-toast-icon"]',
      ) as HTMLElement;

      expect(toast.dataset.variant).toBe(variant);
      expect(icon.className).toContain(expectedClass);
      expect(toast.textContent).toContain('Toast message');
    },
  );

  it('uses assertive alert semantics for error toasts and polite status semantics otherwise', async () => {
    const infoFixture = TestBed.createComponent(TestHostComponent);
    infoFixture.detectChanges();
    await Promise.resolve();
    infoFixture.detectChanges();

    expect(getToast(infoFixture).getAttribute('role')).toBe('status');
    expect(getToast(infoFixture).getAttribute('aria-live')).toBe('polite');

    const errorFixture = TestBed.createComponent(TestHostComponent);
    errorFixture.componentInstance.variant = 'error';
    errorFixture.detectChanges();
    await Promise.resolve();
    errorFixture.detectChanges();

    expect(getToast(errorFixture).getAttribute('role')).toBe('alert');
    expect(getToast(errorFixture).getAttribute('aria-live')).toBe('assertive');
  });

  it('auto-dismisses after the configured duration', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.duration = 1500;
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    vi.advanceTimersByTime(1499);
    fixture.detectChanges();

    expect(fixture.componentInstance.handleDismiss).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[data-testid="mnl-toast"]')).toBeTruthy();

    vi.advanceTimersByTime(301);
    fixture.detectChanges();

    expect(fixture.componentInstance.handleDismiss).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.querySelector('[data-testid="mnl-toast"]')).toBeNull();
  });

  it('hides the dismiss button when dismissible is false', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.dismissible = false;
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="mnl-toast-dismiss"]')).toBeNull();
  });

  it('dismisses immediately when reduced motion is preferred', async () => {
    reducedMotion = true;

    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const dismissButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-toast-dismiss"]',
    ) as HTMLButtonElement;

    dismissButton.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.handleDismiss).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.querySelector('[data-testid="mnl-toast"]')).toBeNull();
  });
});

function createMediaQueryList(reducedMotion: boolean): MediaQueryList {
  return {
    matches: reducedMotion,
    media: '(prefers-reduced-motion: reduce)',
    onchange: null,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
  } as unknown as MediaQueryList;
}

function getToast(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-toast"]') as HTMLElement;
}
