import { ApplicationRef, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlToastService } from './toast.service';

describe('MnlToastService', () => {
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
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  afterEach(() => {
    document.querySelectorAll('[data-mnl-toast-portal]').forEach((element) => element.remove());
    vi.useRealTimers();
    reducedMotion = false;
    TestBed.resetTestingModule();
  });

  it('show displays a toast notification through the outlet portal', async () => {
    const service = TestBed.inject(MnlToastService);

    service.show('Budget saved', { variant: 'success' });
    await Promise.resolve();

    expect(service.activeToasts()).toHaveLength(1);
    expect(document.body.querySelector('[data-testid="mnl-toast-outlet"]')).toBeTruthy();
    expect(document.body.textContent).toContain('Budget saved');
  });

  it('dismiss removes a toast from the active queue and outlet', async () => {
    const service = TestBed.inject(MnlToastService);
    const toastId = service.show('Dismiss me', { duration: 0 });
    await Promise.resolve();

    service.dismiss(toastId);
    await Promise.resolve();

    expect(service.activeToasts()).toHaveLength(0);
    expect(document.body.textContent).not.toContain('Dismiss me');
  });

  it('clear is a no-op when there are no active toasts', () => {
    const service = TestBed.inject(MnlToastService);

    expect(() => service.clear()).not.toThrow();
    expect(service.activeToasts()).toHaveLength(0);
  });

  it('clear removes the active queue and syncs the outlet', async () => {
    const service = TestBed.inject(MnlToastService);
    service.show('Clear me', { duration: 0 });
    await Promise.resolve();

    service.clear();
    await Promise.resolve();

    expect(service.activeToasts()).toHaveLength(0);
    expect(document.body.textContent).not.toContain('Clear me');
  });

  it('ignores dismiss requests for unknown toast ids', async () => {
    const service = TestBed.inject(MnlToastService);
    service.show('Still here', { duration: 0 });
    await Promise.resolve();

    service.dismiss(999);

    expect(service.activeToasts()).toHaveLength(1);
    expect(document.body.textContent).toContain('Still here');
  });

  it('keeps only the latest three visible toasts', async () => {
    const service = TestBed.inject(MnlToastService);

    service.show('First', { duration: 0 });
    service.show('Second', { duration: 0 });
    service.show('Third', { duration: 0 });
    service.show('Fourth', { duration: 0 });
    await Promise.resolve();

    expect(service.activeToasts().map((toast) => toast.message)).toEqual([
      'Second',
      'Third',
      'Fourth',
    ]);
    expect(document.body.textContent).not.toContain('First');
  });

  it('removes a toast when the rendered dismiss button is clicked', async () => {
    reducedMotion = true;

    const service = TestBed.inject(MnlToastService);
    service.show('Closable toast', { duration: 0 });
    await Promise.resolve();

    const dismissButton = document.body.querySelector(
      '[data-testid="mnl-toast-dismiss"]',
    ) as HTMLButtonElement;

    dismissButton.click();
    await Promise.resolve();

    expect(service.activeToasts()).toHaveLength(0);
    expect(document.body.textContent).not.toContain('Closable toast');
  });

  it('can destroy the outlet after it has been attached to the application', async () => {
    const applicationRef = TestBed.inject(ApplicationRef);
    const detachView = vi.spyOn(applicationRef, 'detachView');
    const service = TestBed.inject(MnlToastService);
    service.show('Destroy me', { duration: 0 });
    await Promise.resolve();

    (service as unknown as { destroyOutlet(): void }).destroyOutlet();

    expect(detachView).toHaveBeenCalledTimes(1);
    expect(document.body.querySelector('[data-mnl-toast-portal]')).toBeNull();
  });

  it('allows syncOutlet to no-op before an outlet exists', () => {
    const service = TestBed.inject(MnlToastService);

    expect(() =>
      (service as unknown as { syncOutlet(): void }).syncOutlet(),
    ).not.toThrow();
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
