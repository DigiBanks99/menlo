import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { MnlToastComponent } from './toast.component';
import type { MnlToastEntry } from './toast.types';

@Component({
  selector: 'mnl-toast-outlet',
  standalone: true,
  imports: [MnlToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'contents',
  },
  template: `
    @if (toasts().length) {
      <div
        class="pointer-events-none fixed inset-x-0 top-0 z-[60] flex flex-col items-center gap-3 p-4 sm:items-end"
        data-testid="mnl-toast-outlet"
      >
        @for (toast of toasts(); track toast.id) {
          <mnl-toast
            [dismissible]="toast.dismissible"
            [duration]="toast.duration"
            [message]="toast.message"
            [variant]="toast.variant"
            (dismissed)="handleDismiss(toast.id)"
          ></mnl-toast>
        }
      </div>
    }
  `,
})
export class MnlToastOutletComponent {
  readonly toasts = input<readonly MnlToastEntry[]>([]);

  readonly dismissRequested = output<number>();

  protected handleDismiss(toastId: number): void {
    this.dismissRequested.emit(toastId);
  }
}
