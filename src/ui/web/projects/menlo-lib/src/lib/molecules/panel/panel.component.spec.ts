import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { MnlPanelComponent, type MnlPanelMode } from './panel.component';

@Component({
  standalone: true,
  imports: [MnlPanelComponent],
  template: `
    <button data-testid="outside-button" type="button">Outside</button>

    <mnl-panel [mode]="mode()" [open]="open()" (closed)="handleClosed()">
      <div mnlPanelHeader>
        <h2 class="m-0 text-xl font-semibold">Edit budget item</h2>
      </div>

      <div class="space-y-3">
        <button data-testid="first-action" type="button">First action</button>
        <button data-testid="second-action" type="button">Second action</button>
      </div>
    </mnl-panel>
  `,
})
class TestHostComponent {
  readonly mode = signal<MnlPanelMode>('auto');
  readonly open = signal(true);
  readonly handleClosed = vi.fn(() => this.open.set(false));
}

describe('MnlPanelComponent', () => {
  let desktopViewport = false;
  let reducedMotion = false;

  beforeEach(async () => {
    desktopViewport = false;
    reducedMotion = false;
    vi.useFakeTimers();

    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: vi.fn((query: string) => createMediaQueryList(query, desktopViewport, reducedMotion)),
    });

    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
  });

  afterEach(() => {
    vi.useRealTimers();
    document.body.style.overflow = '';
    desktopViewport = false;
    reducedMotion = false;
  });

  it('renders as a bottom sheet on mobile viewports in auto mode', async () => {
    desktopViewport = false;

    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const panel = getPanel(fixture);

    expect(panel.getAttribute('data-layout')).toBe('sheet');
    expect(panel.className).toContain('translate-y-0');
    expect(fixture.nativeElement.querySelector('[data-testid="mnl-panel-handle"]')).toBeTruthy();
  });

  it('renders as a centered dialog on desktop viewports in auto mode', async () => {
    desktopViewport = true;

    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const panel = getPanel(fixture);

    expect(panel.getAttribute('data-layout')).toBe('dialog');
    expect(panel.className).toContain('scale-100');
    expect(fixture.nativeElement.querySelector('[data-testid="mnl-panel-handle"]')).toBeNull();
  });

  it('forces sheet mode regardless of viewport when mode is sheet', async () => {
    desktopViewport = true;

    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.mode.set('sheet');
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    expect(getPanel(fixture).getAttribute('data-layout')).toBe('sheet');
  });

  it('forces dialog mode regardless of viewport when mode is dialog', async () => {
    desktopViewport = false;

    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.mode.set('dialog');
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    expect(getPanel(fixture).getAttribute('data-layout')).toBe('dialog');
  });

  it('dismisses through backdrop clicks and emits closed', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const backdrop = fixture.nativeElement.querySelector(
      '[data-testid="mnl-panel-backdrop"]',
    ) as HTMLDivElement;
    backdrop.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.handleClosed).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(transitionDuration());
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="mnl-panel"]')).toBeNull();
  });

  it('dismisses through the Escape key and emits closed', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    document.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Escape' }));
    fixture.detectChanges();

    expect(fixture.componentInstance.handleClosed).toHaveBeenCalledTimes(1);
  });

  it('traps focus within the panel while it is open', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const closeButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-panel-close"]',
    ) as HTMLButtonElement;
    const secondAction = fixture.nativeElement.querySelector(
      '[data-testid="second-action"]',
    ) as HTMLButtonElement;

    expect(document.activeElement).toBe(closeButton);

    secondAction.focus();
    document.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Tab' }));

    expect(document.activeElement).toBe(closeButton);

    closeButton.focus();
    document.dispatchEvent(
      new KeyboardEvent('keydown', { bubbles: true, key: 'Tab', shiftKey: true }),
    );

    expect(document.activeElement).toBe(secondAction);
  });

  it('wires ARIA dialog attributes to the projected header content', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const panel = getPanel(fixture);
    const headerId = panel.getAttribute('aria-labelledby');
    const header = headerId ? fixture.nativeElement.querySelector(`#${headerId}`) : null;

    expect(panel.getAttribute('aria-modal')).toBe('true');
    expect(panel.getAttribute('role')).toBe('dialog');
    expect(header?.textContent).toContain('Edit budget item');
  });

  it('locks body scroll while open and restores it after close', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    expect(document.body.style.overflow).toBe('hidden');

    fixture.componentInstance.open.set(false);
    fixture.detectChanges();

    vi.advanceTimersByTime(transitionDuration());
    fixture.detectChanges();

    expect(document.body.style.overflow).toBe('');
  });

  it('removes the panel immediately when reduced motion is preferred', async () => {
    reducedMotion = true;

    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    fixture.componentInstance.open.set(false);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="mnl-panel"]')).toBeNull();
  });
});

function createMediaQueryList(
  query: string,
  desktopViewport: boolean,
  reducedMotion: boolean,
): MediaQueryList {
  const matches = query.includes('min-width') ? desktopViewport : reducedMotion;

  return {
    matches,
    media: query,
    onchange: null,
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    addListener: vi.fn(),
    removeListener: vi.fn(),
    dispatchEvent: vi.fn(),
  } as unknown as MediaQueryList;
}

function getPanel(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-panel"]') as HTMLElement;
}

function transitionDuration(): number {
  return 300;
}
