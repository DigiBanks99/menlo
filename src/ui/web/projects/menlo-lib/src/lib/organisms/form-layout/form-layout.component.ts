import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'mnl-form-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block min-h-full text-mnl-text',
  },
  template: `
    <div class="flex min-h-full flex-col" data-testid="mnl-form-layout">
      <div class="contents" data-testid="mnl-form-layout-title">
        <ng-content select="[mnlFormTitle]"></ng-content>
      </div>

      <div class="flex-1 space-y-6" data-testid="mnl-form-layout-sections">
        <ng-content select=":not([mnlFormTitle]):not([mnlFormActions])"></ng-content>
      </div>

      <footer
        class="sticky bottom-0 z-10 mt-6 border-t border-mnl-border/80 bg-mnl-surface/95 pt-4 supports-[backdrop-filter]:bg-mnl-surface/90 supports-[backdrop-filter]:backdrop-blur"
        data-testid="mnl-form-layout-actions"
      >
        <div class="flex flex-col gap-3 sm:flex-row sm:justify-end">
          <ng-content select="[mnlFormActions]"></ng-content>
        </div>
      </footer>
    </div>
  `,
})
export class MnlFormLayoutComponent {}
