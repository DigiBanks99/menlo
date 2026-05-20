import { ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import type { Meta, StoryObj } from '@storybook/angular';

import { foundationThemes } from '../../foundations/foundation-data';
import { MnlButtonComponent } from '../button';
import { MnlToastComponent } from './toast.component';
import { MnlToastService } from './toast.service';
import type { MnlToastVariant } from './toast.types';

const variants: readonly MnlToastVariant[] = ['success', 'warning', 'error', 'info'];

@Component({
  selector: 'lib-toast-story-preview',
  standalone: true,
  imports: [MnlButtonComponent, MnlToastComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen bg-mnl-bg p-6 text-mnl-text">
      <div class="mx-auto max-w-7xl space-y-6">
        <section class="rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <p class="m-0 text-sm font-semibold uppercase tracking-[0.24em] text-mnl-accent">Atoms</p>
          <h1 class="mt-2 mb-0 text-3xl font-bold tracking-tight">Toast</h1>
          <p class="mt-3 mb-0 max-w-3xl text-sm leading-6 text-mnl-subtext">
            mnl-toast packages feedback into a stacked overlay primitive with semantic variants,
            reduced-motion awareness, and a root-level service portal.
          </p>
        </section>

        <section class="grid gap-4 rounded-2xl bg-mnl-surface p-6 shadow-sm ring-1 ring-mnl-border">
          <div class="space-y-2">
            <h2 class="m-0 text-xl font-semibold">Service demo</h2>
            <p class="m-0 text-sm text-mnl-subtext">
              These buttons trigger MnlToastService.show() so you can preview stacking and
              auto-dismiss behaviour in the Storybook canvas.
            </p>
          </div>

          <div class="flex flex-wrap gap-3">
            @for (variant of variants; track variant) {
              <mnl-button (pressed)="showVariant(variant)" size="sm" variant="secondary">
                Show {{ variant }}
              </mnl-button>
            }

            <mnl-button (pressed)="showStack()" size="sm">Stack three</mnl-button>
          </div>
        </section>

        <div class="grid gap-6 xl:grid-cols-2">
          @for (theme of themes; track theme.mode) {
            <section
              [attr.style]="theme.previewStyle"
              class="space-y-6 rounded-2xl p-6 shadow-sm ring-1 ring-mnl-border"
            >
              <div class="flex items-center justify-between gap-4">
                <div>
                  <h2 class="m-0 text-2xl font-semibold">{{ theme.label }}</h2>
                  <p class="mt-2 mb-0 text-sm text-mnl-subtext">
                    Semantic notifications stay legible in both Latte and Mocha without hardcoded
                    page-specific styling.
                  </p>
                </div>

                <span
                  class="inline-flex items-center rounded-full bg-mnl-surface-alt px-3 py-1 text-xs font-semibold text-mnl-subtext ring-1 ring-mnl-border"
                >
                  {{ theme.mode }}
                </span>
              </div>

              <article class="space-y-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Variants
                </h3>

                <div class="space-y-3">
                  @for (variant of variants; track variant) {
                    <mnl-toast
                      [dismissible]="variant !== 'success'"
                      [duration]="0"
                      [message]="messageFor(variant)"
                      [variant]="variant"
                    ></mnl-toast>
                  }
                </div>
              </article>

              <article class="space-y-4 rounded-2xl bg-mnl-surface p-4 ring-1 ring-mnl-border">
                <h3 class="m-0 text-sm font-semibold uppercase tracking-[0.18em] text-mnl-subtext">
                  Stacking
                </h3>

                <div class="space-y-3">
                  <mnl-toast
                    [duration]="0"
                    message="Budget saved successfully"
                    variant="success"
                  ></mnl-toast>
                  <mnl-toast
                    [duration]="0"
                    message="Review overspent line items"
                    variant="warning"
                  ></mnl-toast>
                  <mnl-toast
                    [duration]="0"
                    message="Failed to save category changes"
                    variant="error"
                  ></mnl-toast>
                </div>
              </article>
            </section>
          }
        </div>
      </div>
    </div>
  `,
})
class ToastStoryPreviewComponent {
  protected readonly themes = foundationThemes;
  protected readonly variants = variants;

  private readonly destroyRef = inject(DestroyRef);
  private readonly toastService = inject(MnlToastService);

  constructor() {
    this.destroyRef.onDestroy(() => this.toastService.clear());
  }

  protected messageFor(variant: MnlToastVariant): string {
    const messages: Record<MnlToastVariant, string> = {
      success: 'Budget saved successfully',
      warning: 'Review overspent line items',
      error: 'Failed to save category changes',
      info: 'Monthly summary is ready to review',
    };

    return messages[variant];
  }

  protected showStack(): void {
    this.toastService.show(this.messageFor('success'), { duration: 1800, variant: 'success' });
    this.toastService.show(this.messageFor('warning'), { duration: 2400, variant: 'warning' });
    this.toastService.show(this.messageFor('info'), { duration: 3000, variant: 'info' });
  }

  protected showVariant(variant: MnlToastVariant): void {
    this.toastService.show(this.messageFor(variant), {
      dismissible: variant !== 'success',
      duration: 2200,
      variant,
    });
  }
}

const meta: Meta<ToastStoryPreviewComponent> = {
  title: 'Atoms/Toast',
  component: ToastStoryPreviewComponent,
  parameters: {
    layout: 'fullscreen',
  },
};

export default meta;

type Story = StoryObj<ToastStoryPreviewComponent>;

export const Overview: Story = {};
