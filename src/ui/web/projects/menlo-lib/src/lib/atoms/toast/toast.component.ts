import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { Check, CircleAlert, Info, LucideAngularModule, TriangleAlert, X } from 'lucide-angular';

import type { MnlToastVariant } from './toast.types';

const transitionDurationMs = 300;
const toastBaseClasses =
  'pointer-events-auto flex w-full items-start gap-3 rounded-2xl border px-4 py-3 shadow-md transition-[transform,opacity] duration-300 ease-out motion-reduce:transition-none';
const toastHiddenClasses = '-translate-y-2 opacity-0';
const toastVisibleClasses = 'translate-y-0 opacity-100';

const variantClasses: Record<MnlToastVariant, string> = {
  success: 'border-mnl-success/30 bg-mnl-surface text-mnl-text ring-1 ring-mnl-success/15',
  warning: 'border-mnl-warning/35 bg-mnl-surface text-mnl-text ring-1 ring-mnl-warning/15',
  error: 'border-mnl-error/35 bg-mnl-surface text-mnl-text ring-1 ring-mnl-error/15',
  info: 'border-mnl-info/35 bg-mnl-surface text-mnl-text ring-1 ring-mnl-info/15',
};

const iconContainerClasses: Record<MnlToastVariant, string> = {
  success: 'bg-mnl-success/15 text-mnl-success',
  warning: 'bg-mnl-warning/20 text-mnl-warning',
  error: 'bg-mnl-error/15 text-mnl-error',
  info: 'bg-mnl-info/15 text-mnl-info',
};

const iconMap = {
  success: Check,
  warning: TriangleAlert,
  error: CircleAlert,
  info: Info,
} as const satisfies Record<MnlToastVariant, unknown>;

@Component({
  selector: 'mnl-toast',
  standalone: true,
  imports: [LucideAngularModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block w-full max-w-sm',
  },
  template: `
    <article
      [attr.aria-atomic]="'true'"
      [attr.aria-live]="ariaLive()"
      [attr.data-variant]="variant()"
      [attr.role]="ariaRole()"
      [class]="toastClasses()"
      data-testid="mnl-toast"
    >
      <span [class]="leadingIconClasses()" aria-hidden="true" data-testid="mnl-toast-icon">
        <lucide-icon [img]="icon()" class="size-4"></lucide-icon>
      </span>

      <p class="m-0 min-w-0 flex-1 text-sm leading-6">
        {{ message() }}
      </p>

      @if (dismissible()) {
        <button
          aria-label="Dismiss notification"
          class="inline-flex size-9 shrink-0 items-center justify-center rounded-xl border border-transparent bg-transparent text-mnl-subtext transition-colors duration-200 hover:bg-mnl-surface-alt hover:text-mnl-text focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-mnl-accent focus-visible:ring-offset-2 focus-visible:ring-offset-mnl-surface motion-reduce:transition-none"
          data-testid="mnl-toast-dismiss"
          type="button"
          (click)="requestDismiss()"
        >
          <lucide-icon [img]="dismissIcon" aria-hidden="true" class="size-4"></lucide-icon>
        </button>
      }
    </article>
  `,
})
export class MnlToastComponent {
  readonly dismissible = input(true);
  readonly duration = input(4000);
  readonly message = input.required<string>();
  readonly variant = input<MnlToastVariant>('info');

  readonly dismissed = output<void>();

  protected readonly dismissIcon = X;
  protected readonly ariaLive = computed(() =>
    this.variant() === 'error' ? 'assertive' : 'polite',
  );
  protected readonly ariaRole = computed(() => (this.variant() === 'error' ? 'alert' : 'status'));
  protected readonly icon = computed(() => iconMap[this.variant()]);
  protected readonly leadingIconClasses = computed(() =>
    [
      'inline-flex size-10 shrink-0 items-center justify-center rounded-xl',
      iconContainerClasses[this.variant()],
    ].join(' '),
  );
  protected readonly toastClasses = computed(() =>
    [
      toastBaseClasses,
      variantClasses[this.variant()],
      this.visible() ? toastVisibleClasses : toastHiddenClasses,
    ].join(' '),
  );

  private readonly destroyRef = inject(DestroyRef);
  private readonly prefersReducedMotion = signal(false);
  private readonly visible = signal(false);

  private autoDismissTimer: ReturnType<typeof setTimeout> | null = null;
  private dismissTimer: ReturnType<typeof setTimeout> | null = null;
  private dismissRequested = false;

  constructor() {
    this.registerReducedMotionPreference();

    effect(() => {
      this.duration();
      this.restartAutoDismissTimer();
    });

    queueMicrotask(() => this.visible.set(true));

    this.destroyRef.onDestroy(() => {
      this.clearAutoDismissTimer();
      this.clearDismissTimer();
    });
  }

  protected requestDismiss(): void {
    if (this.dismissRequested) {
      return;
    }

    this.dismissRequested = true;
    this.clearAutoDismissTimer();

    if (this.prefersReducedMotion()) {
      this.dismissed.emit();
      return;
    }

    this.visible.set(false);
    this.clearDismissTimer();
    this.dismissTimer = setTimeout(() => this.dismissed.emit(), transitionDurationMs);
  }

  private clearAutoDismissTimer(): void {
    if (this.autoDismissTimer == null) {
      return;
    }

    clearTimeout(this.autoDismissTimer);
    this.autoDismissTimer = null;
  }

  private clearDismissTimer(): void {
    if (this.dismissTimer == null) {
      return;
    }

    clearTimeout(this.dismissTimer);
    this.dismissTimer = null;
  }

  private registerReducedMotionPreference(): void {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }

    const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    this.prefersReducedMotion.set(mediaQuery.matches);

    const listener = (event: MediaQueryListEvent) => this.prefersReducedMotion.set(event.matches);

    mediaQuery.addEventListener('change', listener);
    this.destroyRef.onDestroy(() => mediaQuery.removeEventListener('change', listener));
  }

  private restartAutoDismissTimer(): void {
    this.clearAutoDismissTimer();

    if (this.dismissRequested || this.duration() <= 0) {
      return;
    }

    this.autoDismissTimer = setTimeout(() => this.requestDismiss(), this.duration());
  }
}
