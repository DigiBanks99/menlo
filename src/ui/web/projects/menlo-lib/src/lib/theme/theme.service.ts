import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const SYSTEM_THEME_QUERY = '(prefers-color-scheme: dark)';
const STORAGE_KEY = 'menlo.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly view = this.document.defaultView;
  private readonly mediaQuery =
    typeof this.view?.matchMedia === 'function'
      ? this.view.matchMedia(SYSTEM_THEME_QUERY)
      : undefined;
  private readonly systemThemeSignal = signal<Theme>(this.mediaQuery?.matches ? 'dark' : 'light');
  private readonly overrideThemeSignal = signal<Theme | null>(this.readStoredTheme());

  readonly currentTheme = computed(() => this.overrideThemeSignal() ?? this.systemThemeSignal());

  constructor() {
    this.applyTheme(this.currentTheme());

    if (this.mediaQuery) {
      this.mediaQuery.addEventListener('change', this.handleSystemThemeChange);
      this.destroyRef.onDestroy(() => {
        this.mediaQuery?.removeEventListener('change', this.handleSystemThemeChange);
      });
    }
  }

  toggle(): void {
    this.setTheme(this.currentTheme() === 'dark' ? 'light' : 'dark');
  }

  setTheme(theme: Theme): void {
    this.overrideThemeSignal.set(theme);
    this.writeStoredTheme(theme);
    this.applyTheme(theme);
  }

  private readonly handleSystemThemeChange = (event: MediaQueryListEvent): void => {
    this.systemThemeSignal.set(event.matches ? 'dark' : 'light');

    if (this.overrideThemeSignal() === null) {
      this.applyTheme(this.currentTheme());
    }
  };

  private readStoredTheme(): Theme | null {
    const storedTheme = this.view?.localStorage?.getItem(STORAGE_KEY);
    return storedTheme === 'light' || storedTheme === 'dark' ? storedTheme : null;
  }

  private writeStoredTheme(theme: Theme): void {
    this.view?.localStorage?.setItem(STORAGE_KEY, theme);
  }

  private applyTheme(theme: Theme): void {
    this.document.documentElement.classList.toggle('dark', theme === 'dark');
    this.document.documentElement.style.colorScheme = theme;
  }
}
