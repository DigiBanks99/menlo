import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  viewChild,
  type WritableSignal,
} from '@angular/core';
import { LucideAngularModule, X } from 'lucide-angular';

export type MnlPanelMode = 'auto' | 'sheet' | 'dialog';

const transitionDurationMs = 300;
const backdropBaseClasses =
  'absolute inset-0 bg-black/45 transition-opacity duration-300 ease-out motion-reduce:transition-none';
const backdropHiddenClasses = 'opacity-0';
const backdropVisibleClasses = 'opacity-100';
const containerBaseClasses = 'absolute inset-0 flex p-4 sm:p-6';
const containerSheetClasses = 'items-end justify-center';
const containerDialogClasses = 'items-center justify-center';
const panelBaseClasses =
  'pointer-events-auto relative flex max-h-[calc(100dvh-2rem)] w-full flex-col overflow-hidden rounded-[1.75rem] border border-mnl-border/80 bg-mnl-surface text-mnl-text shadow-md shadow-black/15 ring-1 ring-mnl-border/80 transition-[transform,opacity] duration-300 ease-out motion-reduce:transform-none motion-reduce:transition-none';
const panelSheetClasses = 'max-w-2xl';
const panelSheetClosedClasses = 'translate-y-full opacity-100';
const panelSheetOpenClasses = 'translate-y-0 opacity-100';
const panelDialogClasses = 'max-w-lg';
const panelDialogClosedClasses = 'scale-95 opacity-0';
const panelDialogOpenClasses = 'scale-100 opacity-100';

@Component({
  selector: 'mnl-panel',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'contents',
  },
  template: `
    @if (isRendered()) {
      <div class="fixed inset-0 z-50" [attr.data-testid]="rootTestId()">
        <div
          aria-hidden="true"
          [class]="backdropClasses()"
          data-testid="mnl-panel-backdrop"
          (click)="requestClose()"
        ></div>

        <div [class]="containerClasses()">
          <section
            #panelElement
            [attr.aria-labelledby]="headerId"
            [attr.data-layout]="resolvedMode()"
            [class]="panelClasses()"
            aria-modal="true"
            data-testid="mnl-panel"
            role="dialog"
            tabindex="-1"
          >
            @if (isSheetMode()) {
              <div class="flex justify-center px-5 pt-4 sm:px-6" data-testid="mnl-panel-handle">
                <span
                  aria-hidden="true"
                  class="block h-1.5 w-12 rounded-full bg-mnl-border/90"
                ></span>
              </div>
            }

            <header
              [attr.id]="headerId"
              class="flex items-start gap-4 border-b border-mnl-border/80 px-5 py-4 sm:px-6"
              data-testid="mnl-panel-header"
            >
              <div class="min-w-0 flex-1">
                <ng-content select="[mnlPanelHeader]"></ng-content>
              </div>

              <button
                aria-label="Close panel"
                class="inline-flex size-10 shrink-0 items-center justify-center rounded-xl border border-mnl-border bg-mnl-surface-alt text-mnl-subtext transition-colors duration-200 hover:text-mnl-text focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-mnl-accent focus-visible:ring-offset-2 focus-visible:ring-offset-mnl-surface motion-reduce:transition-none"
                data-testid="mnl-panel-close"
                type="button"
                (click)="requestClose()"
              >
                <lucide-icon [img]="closeIcon" aria-hidden="true" class="size-5"></lucide-icon>
              </button>
            </header>

            <div
              class="flex-1 overflow-y-auto px-5 pb-5 pt-4 sm:px-6 sm:pb-6"
              data-testid="mnl-panel-body"
            >
              <ng-content></ng-content>
            </div>
          </section>
        </div>
      </div>
    }
  `,
})
export class MnlPanelComponent {
  readonly open = input(false);
  readonly mode = input<MnlPanelMode>('auto');
  readonly rootTestId = input('mnl-panel-root');

  readonly closed = output<void>();

  protected readonly closeIcon = X;
  protected readonly headerId = `mnl-panel-header-${Math.random().toString(36).slice(2, 10)}`;
  protected readonly isRendered = signal(false);
  protected readonly isActive = signal(false);
  protected readonly isSheetMode = computed(() => this.resolvedMode() === 'sheet');
  protected readonly resolvedMode = computed<MnlPanelMode>(() => {
    const mode = this.mode();

    if (mode === 'sheet' || mode === 'dialog') {
      return mode;
    }

    return this.isDesktopViewport() ? 'dialog' : 'sheet';
  });
  protected readonly backdropClasses = computed(() =>
    [backdropBaseClasses, this.isActive() ? backdropVisibleClasses : backdropHiddenClasses].join(
      ' ',
    ),
  );
  protected readonly containerClasses = computed(() =>
    [
      containerBaseClasses,
      this.isSheetMode() ? containerSheetClasses : containerDialogClasses,
    ].join(' '),
  );
  protected readonly panelClasses = computed(() => {
    const layout = this.resolvedMode();

    return [
      panelBaseClasses,
      layout === 'sheet' ? panelSheetClasses : panelDialogClasses,
      layout === 'sheet'
        ? this.isActive()
          ? panelSheetOpenClasses
          : panelSheetClosedClasses
        : this.isActive()
          ? panelDialogOpenClasses
          : panelDialogClosedClasses,
    ].join(' ');
  });

  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly panelElement = viewChild<ElementRef<HTMLElement>>('panelElement');
  private readonly isDesktopViewport = signal(false);
  private readonly prefersReducedMotion = signal(false);
  private readonly focusableSelector =
    'button:not([disabled]), [href], input:not([disabled]):not([type="hidden"]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

  private closeTimer: ReturnType<typeof setTimeout> | null = null;
  private restoreBodyOverflow = '';
  private bodyScrollLocked = false;
  private restoreFocusTarget: HTMLElement | null = null;

  constructor() {
    this.registerMediaQuery('(min-width: 1024px)', this.isDesktopViewport);
    this.registerMediaQuery('(prefers-reduced-motion: reduce)', this.prefersReducedMotion);

    effect(
      () => {
        if (this.open()) {
          this.mountPanel();
          return;
        }

        this.beginClose();
      },
      { allowSignalWrites: true },
    );

    effect(() => {
      if (this.isRendered()) {
        this.lockBodyScroll();
        return;
      }

      this.unlockBodyScroll();
    });

    this.destroyRef.onDestroy(() => {
      this.clearCloseTimer();
      this.unlockBodyScroll();
      this.restoreFocus();
    });
  }

  @HostListener('document:focusin', ['$event'])
  protected handleDocumentFocusIn(event: FocusEvent): void {
    if (!this.isActive()) {
      return;
    }

    const panel = this.panelElement()?.nativeElement;
    const target = event.target as Node | null;

    if (!panel || !target || panel.contains(target)) {
      return;
    }

    this.focusInitialElement();
  }

  @HostListener('document:keydown', ['$event'])
  protected handleDocumentKeydown(event: KeyboardEvent): void {
    if (!this.isActive()) {
      return;
    }

    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      this.requestClose();
      return;
    }

    if (event.key !== 'Tab') {
      return;
    }

    this.trapFocus(event);
  }

  protected requestClose(): void {
    this.closed.emit();
  }

  private beginClose(): void {
    this.isActive.set(false);

    if (!this.isRendered()) {
      return;
    }

    this.clearCloseTimer();

    if (this.prefersReducedMotion()) {
      this.finishClose();
      return;
    }

    this.closeTimer = setTimeout(() => this.finishClose(), transitionDurationMs);
  }

  private captureRestoreFocusTarget(): void {
    const activeElement = this.document.activeElement;
    this.restoreFocusTarget = activeElement instanceof HTMLElement ? activeElement : null;
  }

  private clearCloseTimer(): void {
    if (this.closeTimer == null) {
      return;
    }

    clearTimeout(this.closeTimer);
    this.closeTimer = null;
  }

  private finishClose(): void {
    this.isRendered.set(false);
    this.clearCloseTimer();
    this.restoreFocus();
  }

  private focusInitialElement(): void {
    const panel = this.panelElement()?.nativeElement;

    if (!panel) {
      return;
    }

    const focusableElements = this.getFocusableElements();
    (focusableElements[0] ?? panel).focus();
  }

  private getFocusableElements(): HTMLElement[] {
    const panel = this.panelElement()?.nativeElement;

    if (!panel) {
      return [];
    }

    return Array.from(panel.querySelectorAll<HTMLElement>(this.focusableSelector)).filter(
      (element) =>
        !element.hasAttribute('disabled') &&
        element.tabIndex !== -1 &&
        element.getAttribute('aria-hidden') !== 'true',
    );
  }

  private lockBodyScroll(): void {
    if (this.bodyScrollLocked) {
      return;
    }

    this.restoreBodyOverflow = this.document.body.style.overflow;
    this.document.body.style.overflow = 'hidden';
    this.bodyScrollLocked = true;
  }

  private mountPanel(): void {
    this.clearCloseTimer();

    if (!this.isRendered()) {
      this.captureRestoreFocusTarget();
      this.isRendered.set(true);
    }

    queueMicrotask(() => {
      if (!this.open()) {
        return;
      }

      this.isActive.set(true);
      this.focusInitialElement();
    });
  }

  private registerMediaQuery(query: string, targetSignal: WritableSignal<boolean>): void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }

    const mediaQuery = window.matchMedia(query);
    targetSignal.set(mediaQuery.matches);

    const listener = (event: MediaQueryListEvent) => targetSignal.set(event.matches);

    mediaQuery.addEventListener('change', listener);
    this.destroyRef.onDestroy(() => mediaQuery.removeEventListener('change', listener));
  }

  private restoreFocus(): void {
    if (!this.restoreFocusTarget) {
      return;
    }

    if (this.document.contains(this.restoreFocusTarget)) {
      this.restoreFocusTarget.focus();
    }

    this.restoreFocusTarget = null;
  }

  private trapFocus(event: KeyboardEvent): void {
    const panel = this.panelElement()?.nativeElement;

    if (!panel) {
      return;
    }

    const focusableElements = this.getFocusableElements();

    if (focusableElements.length === 0) {
      event.preventDefault();
      panel.focus();
      return;
    }

    const activeElement = this.document.activeElement as HTMLElement | null;
    const firstElement = focusableElements[0];
    const lastElement = focusableElements.at(-1) ?? firstElement;

    if (event.shiftKey) {
      if (!activeElement || !panel.contains(activeElement) || activeElement === firstElement) {
        event.preventDefault();
        lastElement.focus();
      }

      return;
    }

    if (!activeElement || !panel.contains(activeElement) || activeElement === lastElement) {
      event.preventDefault();
      firstElement.focus();
    }
  }

  private unlockBodyScroll(): void {
    if (!this.bodyScrollLocked) {
      return;
    }

    this.document.body.style.overflow = this.restoreBodyOverflow;
    this.restoreBodyOverflow = '';
    this.bodyScrollLocked = false;
  }
}
