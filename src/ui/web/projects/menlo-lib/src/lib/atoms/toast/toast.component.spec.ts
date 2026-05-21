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
  let mediaQueryList: MediaQueryList & { setMatches(matches: boolean): void };
  let reducedMotion = false;

  beforeEach(async () => {
    reducedMotion = false;
    vi.useFakeTimers();
    mediaQueryList = createMediaQueryList(reducedMotion);

    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: vi.fn(() => mediaQueryList),
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
    mediaQueryList = createMediaQueryList(reducedMotion);

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

  it('does not schedule auto-dismiss when the duration is zero', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.duration = 0;
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    vi.advanceTimersByTime(5000);
    fixture.detectChanges();

    expect(fixture.componentInstance.handleDismiss).not.toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('[data-testid="mnl-toast"]')).toBeTruthy();
  });

  it('waits for the transition duration before dismissing without reduced motion', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const dismissButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-toast-dismiss"]',
    ) as HTMLButtonElement;

    dismissButton.click();
    fixture.detectChanges();

    expect(getToast(fixture).className).toContain('opacity-0');

    vi.advanceTimersByTime(299);
    fixture.detectChanges();
    expect(fixture.componentInstance.handleDismiss).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);
    fixture.detectChanges();
    expect(fixture.componentInstance.handleDismiss).toHaveBeenCalledTimes(1);
  });

  it('ignores duplicate dismiss requests once dismissal has started', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const dismissButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-toast-dismiss"]',
    ) as HTMLButtonElement;

    dismissButton.click();
    dismissButton.click();
    vi.advanceTimersByTime(transitionDuration());
    fixture.detectChanges();

    expect(fixture.componentInstance.handleDismiss).toHaveBeenCalledTimes(1);
  });

  it('restarts the auto-dismiss timer when the duration input changes', async () => {
    const fixture = TestBed.createComponent(MnlToastComponent);
    const handleDismiss = vi.fn();
    fixture.componentInstance.dismissed.subscribe(handleDismiss);
    fixture.componentRef.setInput('message', 'Toast message');
    fixture.componentRef.setInput('duration', 1000);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    fixture.componentRef.setInput('duration', 3000);
    fixture.detectChanges();

    vi.advanceTimersByTime(1000);
    fixture.detectChanges();
    expect(handleDismiss).not.toHaveBeenCalled();

    vi.advanceTimersByTime(2301);
    fixture.detectChanges();
    expect(handleDismiss).toHaveBeenCalledTimes(1);
  });

  it('removes the reduced-motion listener on destroy', () => {
    const removeEventListener = vi.spyOn(mediaQueryList, 'removeEventListener');
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();

    fixture.destroy();

    expect(removeEventListener).toHaveBeenCalledWith('change', expect.any(Function));
  });

  it('skips reduced-motion registration when matchMedia is unavailable', async () => {
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: undefined,
    });

    const fixture = TestBed.createComponent(MnlToastComponent);
    const handleDismiss = vi.fn();
    fixture.componentInstance.dismissed.subscribe(handleDismiss);
    fixture.componentRef.setInput('message', 'Toast message');
    fixture.componentRef.setInput('duration', 10);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    vi.advanceTimersByTime(311);
    fixture.detectChanges();

    expect(handleDismiss).toHaveBeenCalledTimes(1);
  });

  it('reacts to reduced-motion preference changes after creation', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    mediaQueryList.setMatches(true);

    const dismissButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-toast-dismiss"]',
    ) as HTMLButtonElement;
    dismissButton.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.handleDismiss).toHaveBeenCalledTimes(1);
  });
});

function createMediaQueryList(reducedMotion: boolean): MediaQueryList & { setMatches(matches: boolean): void } {
  const listeners = new Set<(event: MediaQueryListEvent) => void>();
  const mediaQueryList = {
    matches: reducedMotion,
    media: '(prefers-reduced-motion: reduce)',
    onchange: null,
    addEventListener: vi.fn((event: string, listener: (event: MediaQueryListEvent) => void) => {
      if (event === 'change') {
        listeners.add(listener);
      }
    }),
    removeEventListener: vi.fn((event: string, listener: (event: MediaQueryListEvent) => void) => {
      if (event === 'change') {
        listeners.delete(listener);
      }
    }),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
    setMatches(nextMatches: boolean) {
      this.matches = nextMatches;
      for (const listener of listeners) {
        listener({ matches: nextMatches } as MediaQueryListEvent);
      }
    },
  };

  return mediaQueryList as MediaQueryList & { setMatches(matches: boolean): void };
}

function getToast(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-toast"]') as HTMLElement;
}

function transitionDuration(): number {
  return 300;
}
