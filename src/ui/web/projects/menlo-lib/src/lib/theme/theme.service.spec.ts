import { DOCUMENT } from '@angular/common';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ThemeService } from './theme.service';

describe('ThemeService', () => {
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

  it('should initialize from the current OS preference when no override exists', () => {
    mediaQueryList.setMatches(true);

    const service = TestBed.inject(ThemeService);

    expect(service.currentTheme()).toBe('dark');
    expect(htmlElement.classList.contains('dark')).toBe(true);
    expect(htmlElement.style.colorScheme).toBe('dark');
  });

  it('should initialize from the stored override before the OS preference', () => {
    mediaQueryList.setMatches(true);
    storage.setItem('menlo.theme', 'light');

    const service = TestBed.inject(ThemeService);

    expect(service.currentTheme()).toBe('light');
    expect(htmlElement.classList.contains('dark')).toBe(false);
    expect(htmlElement.style.colorScheme).toBe('light');
  });

  it('should write the override, update the signal, and toggle the html class when setting the theme', () => {
    const service = TestBed.inject(ThemeService);

    service.setTheme('dark');

    expect(service.currentTheme()).toBe('dark');
    expect(storage.getItem('menlo.theme')).toBe('dark');
    expect(htmlElement.classList.contains('dark')).toBe(true);
    expect(htmlElement.style.colorScheme).toBe('dark');
  });

  it('should toggle between light and dark themes', () => {
    const service = TestBed.inject(ThemeService);

    service.toggle();
    expect(service.currentTheme()).toBe('dark');

    service.toggle();
    expect(service.currentTheme()).toBe('light');
  });

  it('should react to OS theme changes when there is no stored override', () => {
    const service = TestBed.inject(ThemeService);

    mediaQueryList.dispatchChange(true);

    expect(service.currentTheme()).toBe('dark');
    expect(htmlElement.classList.contains('dark')).toBe(true);
  });

  it('should switch back to light when the OS preference changes to light without an override', () => {
    mediaQueryList.setMatches(true);
    const service = TestBed.inject(ThemeService);

    mediaQueryList.dispatchChange(false);

    expect(service.currentTheme()).toBe('light');
    expect(htmlElement.classList.contains('dark')).toBe(false);
  });

  it('should ignore OS theme changes when an explicit override exists', () => {
    storage.setItem('menlo.theme', 'light');
    const service = TestBed.inject(ThemeService);

    mediaQueryList.dispatchChange(true);

    expect(service.currentTheme()).toBe('light');
    expect(htmlElement.classList.contains('dark')).toBe(false);
  });

  it('should default to light when matchMedia is unavailable', () => {
    TestBed.resetTestingModule();
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
            },
          },
        },
      ],
    });

    const service = TestBed.inject(ThemeService);

    expect(service.currentTheme()).toBe('light');
    expect(htmlElement.classList.contains('dark')).toBe(false);
  });
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

  dispatchChange(matches: boolean): void {
    this.matches = matches;
    const event = { matches, media: this.media } as MediaQueryListEvent;

    this.onchange?.call(this, event);
    this.listeners.forEach((listener) => listener(event));
  }

  setMatches(matches: boolean): void {
    this.matches = matches;
  }
}
