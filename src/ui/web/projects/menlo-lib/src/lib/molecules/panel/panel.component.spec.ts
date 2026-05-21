import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { By } from '@angular/platform-browser';
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
  const mediaQueryLists = new Map<string, MediaQueryList & { setMatches(matches: boolean): void }>();
  let desktopViewport = false;
  let reducedMotion = false;

  beforeEach(async () => {
    desktopViewport = false;
    reducedMotion = false;
    vi.useFakeTimers();
    mediaQueryLists.clear();

    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: vi.fn((query: string) => {
        const mediaQueryList = createMediaQueryList(query, desktopViewport, reducedMotion);
        mediaQueryLists.set(query, mediaQueryList);
        return mediaQueryList;
      }),
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

  it('ignores non-Escape and non-Tab keyboard input while open', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    document.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Enter' }));
    fixture.detectChanges();

    expect(fixture.componentInstance.handleClosed).not.toHaveBeenCalled();
  });

  it('ignores document focus and keydown events while inactive', () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.componentInstance.open.set(false);
    fixture.detectChanges();

    document.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));
    document.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true, key: 'Escape' }));

    expect(fixture.componentInstance.handleClosed).not.toHaveBeenCalled();
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

  it('moves focus back into the panel when focus escapes', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const outsideButton = fixture.nativeElement.querySelector(
      '[data-testid="outside-button"]',
    ) as HTMLButtonElement;
    const closeButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-panel-close"]',
    ) as HTMLButtonElement;

    outsideButton.focus();
    document.dispatchEvent(new FocusEvent('focusin', { bubbles: true }));

    expect(document.activeElement).toBe(closeButton);
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

  it('renders a custom root test id when requested', async () => {
    const fixture = TestBed.createComponent(MnlPanelComponent);
    fixture.componentRef.setInput('open', true);
    fixture.componentRef.setInput('rootTestId', 'custom-panel-root');
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="custom-panel-root"]')).toBeTruthy();
  });

  it('emits closed when the header close button is clicked', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const closeButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-panel-close"]',
    ) as HTMLButtonElement;
    closeButton.click();

    expect(fixture.componentInstance.handleClosed).toHaveBeenCalledTimes(1);
  });

  it('responds to media-query changes after the panel is created', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    mediaQueryLists.get('(min-width: 1024px)')?.setMatches(true);
    fixture.detectChanges();

    expect(getPanel(fixture).getAttribute('data-layout')).toBe('dialog');
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

  it('skips media-query registration when matchMedia is unavailable', async () => {
    Object.defineProperty(window, 'matchMedia', {
      configurable: true,
      writable: true,
      value: undefined,
    });

    const fixture = TestBed.createComponent(MnlPanelComponent);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[data-testid="mnl-panel"]')).toBeTruthy();
  });

  it('supports internal focus helpers when the panel is not mounted', () => {
    const fixture = TestBed.createComponent(MnlPanelComponent);
    fixture.detectChanges();

    expect(
      (fixture.componentInstance as unknown as { getFocusableElements(): HTMLElement[] }).getFocusableElements(),
    ).toEqual([]);

    expect(() =>
      (fixture.componentInstance as unknown as { focusInitialElement(): void }).focusInitialElement(),
    ).not.toThrow();

    const event = { preventDefault: vi.fn(), shiftKey: false } as unknown as KeyboardEvent;
    expect(() =>
      (fixture.componentInstance as unknown as { trapFocus(event: KeyboardEvent): void }).trapFocus(event),
    ).not.toThrow();
  });

  it('captures a null restore-focus target when the active element is not an HTMLElement', () => {
    const fixture = TestBed.createComponent(MnlPanelComponent);
    fixture.detectChanges();
    const originalDescriptor = Object.getOwnPropertyDescriptor(document, 'activeElement');

    Object.defineProperty(document, 'activeElement', {
      configurable: true,
      get: () => ({ nodeType: 1 }),
    });

    (
      fixture.componentInstance as unknown as {
        captureRestoreFocusTarget(): void;
        restoreFocusTarget: HTMLElement | null;
      }
    ).captureRestoreFocusTarget();

    expect(
      (fixture.componentInstance as unknown as { restoreFocusTarget: HTMLElement | null })
        .restoreFocusTarget,
    ).toBeNull();

    if (originalDescriptor) {
      Object.defineProperty(document, 'activeElement', originalDescriptor);
    } else {
      Reflect.deleteProperty(document, 'activeElement');
    }
  });

  it('focuses the panel itself when no focusable children are available', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const panel = getPanel(fixture);
    const event = { preventDefault: vi.fn(), shiftKey: false } as unknown as KeyboardEvent;
    const component = getPanelComponent(fixture) as unknown as {
      trapFocus(event: KeyboardEvent): void;
      getFocusableElements(): HTMLElement[];
    };
    vi.spyOn(component, 'getFocusableElements').mockReturnValue([]);

    component.trapFocus(event);

    expect(event.preventDefault).toHaveBeenCalledTimes(1);
    expect(document.activeElement).toBe(panel);
  });

  it('focuses the panel shell when initial focus has no focusable children available', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const panel = getPanel(fixture);
    const component = getPanelComponent(fixture) as unknown as {
      focusInitialElement(): void;
      getFocusableElements(): HTMLElement[];
    };
    vi.spyOn(component, 'getFocusableElements').mockReturnValue([]);

    component.focusInitialElement();

    expect(document.activeElement).toBe(panel);
  });

  it('does not wrap focus when tabbing forward from a non-terminal element', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const closeButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-panel-close"]',
    ) as HTMLButtonElement;
    const event = { preventDefault: vi.fn(), shiftKey: false } as unknown as KeyboardEvent;
    const component = getPanelComponent(fixture) as unknown as {
      trapFocus(event: KeyboardEvent): void;
    };

    closeButton.focus();
    component.trapFocus(event);

    expect(event.preventDefault).not.toHaveBeenCalled();
    expect(document.activeElement).toBe(closeButton);
  });

  it('does not wrap focus when shift-tabbing from a non-leading element', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const secondAction = fixture.nativeElement.querySelector(
      '[data-testid="second-action"]',
    ) as HTMLButtonElement;
    const event = { preventDefault: vi.fn(), shiftKey: true } as unknown as KeyboardEvent;
    const component = getPanelComponent(fixture) as unknown as {
      trapFocus(event: KeyboardEvent): void;
    };

    secondAction.focus();
    component.trapFocus(event);

    expect(event.preventDefault).not.toHaveBeenCalled();
    expect(document.activeElement).toBe(secondAction);
  });

  it('falls back to the first focusable element when lastElement cannot be derived', async () => {
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    const closeButton = fixture.nativeElement.querySelector(
      '[data-testid="mnl-panel-close"]',
    ) as HTMLButtonElement;
    const focusableElements = [closeButton] as HTMLElement[];
    Object.defineProperty(focusableElements, 'at', {
      configurable: true,
      value: () => undefined,
    });

    const event = { preventDefault: vi.fn(), shiftKey: false } as unknown as KeyboardEvent;
    const component = getPanelComponent(fixture) as unknown as {
      trapFocus(event: KeyboardEvent): void;
      getFocusableElements(): HTMLElement[];
    };
    vi.spyOn(component, 'getFocusableElements').mockReturnValue(focusableElements);

    closeButton.focus();
    component.trapFocus(event);

    expect(event.preventDefault).toHaveBeenCalledTimes(1);
    expect(document.activeElement).toBe(closeButton);
  });

  it('restores focus state cleanly when the previous target is no longer in the document', () => {
    const fixture = TestBed.createComponent(MnlPanelComponent);
    fixture.detectChanges();

    (fixture.componentInstance as unknown as { restoreFocusTarget: HTMLElement | null }).restoreFocusTarget =
      document.createElement('button');

    expect(() =>
      (fixture.componentInstance as unknown as { restoreFocus(): void }).restoreFocus(),
    ).not.toThrow();
    expect(
      (fixture.componentInstance as unknown as { restoreFocusTarget: HTMLElement | null }).restoreFocusTarget,
    ).toBeNull();
  });

  it('does not relock body scroll when it is already locked', () => {
    const fixture = TestBed.createComponent(MnlPanelComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as {
      lockBodyScroll(): void;
      bodyScrollLocked: boolean;
      restoreBodyOverflow: string;
    };
    component.lockBodyScroll();
    const originalOverflow = component.restoreBodyOverflow;
    document.body.style.overflow = 'hidden';

    component.lockBodyScroll();

    expect(component.bodyScrollLocked).toBe(true);
    expect(component.restoreBodyOverflow).toBe(originalOverflow);
  });

  it('abandons activation if the panel is no longer open when the mount microtask runs', async () => {
    const fixture = TestBed.createComponent(MnlPanelComponent);
    const component = fixture.componentInstance as unknown as {
      mountPanel(): void;
      isActive(): boolean;
    };

    component.mountPanel();
    await Promise.resolve();
    fixture.detectChanges();

    expect(component.isActive()).toBe(false);
  });
});

function createMediaQueryList(
  query: string,
  desktopViewport: boolean,
  reducedMotion: boolean,
): MediaQueryList & { setMatches(matches: boolean): void } {
  const matches = query.includes('min-width') ? desktopViewport : reducedMotion;
  const listeners = new Set<(event: MediaQueryListEvent) => void>();
  const mediaQueryList = {
    matches,
    media: query,
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

function getPanel(fixture: { nativeElement: HTMLElement }): HTMLElement {
  return fixture.nativeElement.querySelector('[data-testid="mnl-panel"]') as HTMLElement;
}

function getPanelComponent(fixture: unknown): MnlPanelComponent {
  return (fixture as { debugElement: { query(selector: unknown): { componentInstance: MnlPanelComponent } } })
    .debugElement.query(By.directive(MnlPanelComponent)).componentInstance;
}

function transitionDuration(): number {
  return 300;
}
