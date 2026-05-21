import { DOCUMENT } from '@angular/common';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import {
  createMnlChartOptions,
  getMnlChartColors,
  MNL_CHART_PALETTE,
  MNL_DARK_CHART_COLORS,
  MNL_LIGHT_CHART_COLORS,
} from './chart-palette';
import { ThemeService } from './theme.service';

describe('chart palette', () => {
  let mediaQueryList: MockMediaQueryList;
  let storage: MockStorage;
  let htmlElement: HTMLElement;

  beforeEach(() => {
    mediaQueryList = new MockMediaQueryList(false);
    storage = new MockStorage();
    htmlElement = window.document.createElement('html');

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: DOCUMENT,
          useValue: {
            documentElement: htmlElement,
            defaultView: {
              localStorage: storage,
              matchMedia: vi.fn(() => mediaQueryList),
            },
          },
        },
      ],
    });
  });

  it('matches the Catppuccin light palette snapshot', () => {
    expect(Array.from(MNL_LIGHT_CHART_COLORS)).toMatchInlineSnapshot(`
      [
        "#ea76cb",
        "#8839ef",
        "#7287fd",
        "#fe640b",
        "#40a02b",
        "#df8e1d",
        "#dc8a78",
        "#d20f39",
      ]
    `);
  });

  it('matches the Catppuccin dark palette snapshot', () => {
    expect(Array.from(MNL_DARK_CHART_COLORS)).toMatchInlineSnapshot(`
      [
        "#f5c2e7",
        "#cba6f7",
        "#b4befe",
        "#fab387",
        "#a6e3a1",
        "#f9e2af",
        "#f5e0dc",
        "#f38ba8",
      ]
    `);
  });

  it('returns the expected palette for each theme', () => {
    expect(getMnlChartColors('light')).toBe(MNL_LIGHT_CHART_COLORS);
    expect(getMnlChartColors('dark')).toBe(MNL_DARK_CHART_COLORS);
  });

  it('reactively switches the injected palette when the theme changes', () => {
    const themeService = TestBed.inject(ThemeService);
    const chartPalette = TestBed.inject(MNL_CHART_PALETTE);

    expect(chartPalette.colors()).toEqual(MNL_LIGHT_CHART_COLORS);

    themeService.setTheme('dark');

    expect(chartPalette.colors()).toEqual(MNL_DARK_CHART_COLORS);
  });

  it.each([
    ['light', '#4c4f69', '#6c6f85', '#bcc0cc', 'light'],
    ['dark', '#cdd6f4', '#a6adc8', '#6c7086', 'dark'],
  ] as const)(
    'builds themed default chart options for %s mode',
    (theme, expectedText, expectedSubtext, expectedBorder, expectedTooltipTheme) => {
      const options = createMnlChartOptions(theme);

      expect(options.chart.fontFamily).toContain('Nunito Sans');
      expect(options.chart.foreColor).toBe(expectedText);
      expect(options.chart.background).toBe('transparent');
      expect(options.grid.borderColor).toBe(expectedBorder);
      expect(options.legend.labels?.colors).toBe(expectedText);
      expect(options.tooltip.theme).toBe(expectedTooltipTheme);
      expect(options.tooltip.style?.fontFamily).toContain('Nunito Sans');
      expect(options.xaxis.labels?.style?.colors).toBe(expectedSubtext);
      expect(options.yaxis.labels?.style?.colors).toBe(expectedSubtext);
      expect(options.yaxis.title?.style?.color).toBe(expectedSubtext);
      expect(options.theme.mode).toBe(expectedTooltipTheme);
    },
  );
});

class MockStorage implements Storage {
  private readonly store = new Map<string, string>();

  get length(): number {
    return this.store.size;
  }

  clear(): void {
    this.store.clear();
  }

  getItem(key: string): string | null {
    return this.store.get(key) ?? null;
  }

  key(index: number): string | null {
    return Array.from(this.store.keys())[index] ?? null;
  }

  removeItem(key: string): void {
    this.store.delete(key);
  }

  setItem(key: string, value: string): void {
    this.store.set(key, value);
  }
}

class MockMediaQueryList implements MediaQueryList {
  media = '(prefers-color-scheme: dark)';
  onchange: ((this: MediaQueryList, event: MediaQueryListEvent) => unknown) | null = null;

  private readonly listeners = new Set<(event: MediaQueryListEvent) => void>();

  constructor(public matches: boolean) {}

  addEventListener(type: string, listener: EventListenerOrEventListenerObject | null): void {
    if (type === 'change' && typeof listener === 'function') {
      this.listeners.add(listener as (event: MediaQueryListEvent) => void);
    }
  }

  removeEventListener(type: string, listener: EventListenerOrEventListenerObject | null): void {
    if (type === 'change' && typeof listener === 'function') {
      this.listeners.delete(listener as (event: MediaQueryListEvent) => void);
    }
  }

  addListener(
    listener: ((this: MediaQueryList, event: MediaQueryListEvent) => unknown) | null,
  ): void {
    if (listener) {
      this.listeners.add(listener.bind(this));
    }
  }

  removeListener(
    listener: ((this: MediaQueryList, event: MediaQueryListEvent) => unknown) | null,
  ): void {
    if (listener) {
      this.listeners.forEach((registeredListener) => {
        if (registeredListener === listener) {
          this.listeners.delete(registeredListener);
        }
      });
    }
  }

  dispatchEvent(): boolean {
    return true;
  }
}
