import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export const mnlPageHeaderDefaultGradient =
  'linear-gradient(135deg, var(--mnl-color-gradient-start) 0%, var(--mnl-color-gradient-mid) 56%, var(--mnl-color-gradient-end) 100%)';

@Component({
  selector: 'mnl-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block',
  },
  template: `
    <section class="relative -mx-4 -mt-6 sm:-mx-6 lg:mx-0" data-testid="mnl-page-header">
      <div class="overflow-hidden rounded-b-2xl" data-testid="mnl-page-header-shell">
        <div
          [style.background-image]="resolvedGradient()"
          class="min-h-[12.5rem] bg-cover bg-no-repeat text-mnl-mocha-crust"
          data-testid="mnl-page-header-gradient"
        >
          <div
            class="mx-auto flex min-h-[12.5rem] max-w-screen-2xl flex-col gap-6 px-4 pb-20 pt-6 sm:px-6 lg:px-8"
          >
            <ng-content select="[mnlPageHeaderHero]"></ng-content>
          </div>
        </div>
      </div>

      <div class="relative -mt-14 px-4 pb-2 sm:px-6 lg:px-8" data-testid="mnl-page-header-overlap">
        <div class="mx-auto w-full max-w-screen-2xl empty:hidden">
          <ng-content></ng-content>
        </div>
      </div>
    </section>
  `,
})
export class MnlPageHeaderComponent {
  readonly gradient = input(mnlPageHeaderDefaultGradient);

  protected readonly resolvedGradient = computed(() => {
    const value = this.gradient().trim();
    return value || mnlPageHeaderDefaultGradient;
  });
}
