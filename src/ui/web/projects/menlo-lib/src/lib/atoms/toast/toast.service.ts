import { DOCUMENT } from '@angular/common';
import {
  ApplicationRef,
  ComponentRef,
  DestroyRef,
  EnvironmentInjector,
  Injectable,
  createComponent,
  inject,
  signal,
} from '@angular/core';

import { MnlToastOutletComponent } from './toast-outlet.component';
import type { MnlToastEntry, MnlToastOptions } from './toast.types';

const defaultDurationMs = 4000;
const maxVisibleToasts = 3;

@Injectable({ providedIn: 'root' })
export class MnlToastService {
  private readonly applicationRef = inject(ApplicationRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);
  private readonly environmentInjector = inject(EnvironmentInjector);
  private readonly toasts = signal<readonly MnlToastEntry[]>([]);
  readonly activeToasts = this.toasts.asReadonly();

  private hostElement: HTMLElement | null = null;
  private nextToastId = 0;
  private outletDismissSubscription: { unsubscribe(): void } | null = null;
  private outletRef: ComponentRef<MnlToastOutletComponent> | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.destroyOutlet());
  }

  clear(): void {
    if (this.activeToasts().length === 0) {
      return;
    }

    this.toasts.set([]);
    this.syncOutlet();
  }

  dismiss(toastId: number): void {
    const nextToasts = this.activeToasts().filter((toast) => toast.id !== toastId);

    if (nextToasts.length === this.activeToasts().length) {
      return;
    }

    this.toasts.set(nextToasts);
    this.syncOutlet();
  }

  show(message: string, options: MnlToastOptions = {}): number {
    this.ensureOutlet();

    const toastId = ++this.nextToastId;
    const toast: MnlToastEntry = {
      dismissible: options.dismissible ?? true,
      duration: options.duration ?? defaultDurationMs,
      id: toastId,
      message,
      variant: options.variant ?? 'info',
    };

    this.toasts.update((currentToasts) => [...currentToasts, toast].slice(-maxVisibleToasts));
    this.syncOutlet();

    return toastId;
  }

  private destroyOutlet(): void {
    this.outletDismissSubscription?.unsubscribe();
    this.outletDismissSubscription = null;

    if (this.outletRef) {
      if (!this.applicationRef.destroyed) {
        this.applicationRef.detachView(this.outletRef.hostView);
      }

      this.outletRef.destroy();
      this.outletRef = null;
    }

    this.hostElement?.remove();
    this.hostElement = null;
  }

  private ensureOutlet(): void {
    if (this.outletRef || !this.document.body) {
      return;
    }

    this.hostElement = this.document.createElement('div');
    this.hostElement.setAttribute('data-mnl-toast-portal', 'true');
    this.document.body.appendChild(this.hostElement);

    this.outletRef = createComponent(MnlToastOutletComponent, {
      environmentInjector: this.environmentInjector,
      hostElement: this.hostElement,
    });

    this.applicationRef.attachView(this.outletRef.hostView);
    this.outletDismissSubscription = this.outletRef.instance.dismissRequested.subscribe((toastId) =>
      this.dismiss(toastId),
    );

    this.syncOutlet();
  }

  private syncOutlet(): void {
    if (!this.outletRef) {
      return;
    }

    this.outletRef.setInput('toasts', this.activeToasts());
    this.outletRef.changeDetectorRef.detectChanges();
  }
}
