import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type MnlCardPadding = 'sm' | 'md' | 'lg';

const cardBaseClasses =
  'flex h-full flex-col overflow-hidden rounded-2xl bg-mnl-surface text-mnl-text shadow-sm ring-1 ring-mnl-border/70 transition-[transform,box-shadow] duration-200 motion-reduce:transform-none motion-reduce:transition-none';
const interactiveClasses = 'cursor-pointer hover:-translate-y-0.5 hover:shadow-md';

const sectionPaddingClasses: Record<MnlCardPadding, string> = {
  sm: 'px-3 py-3',
  md: 'px-4 py-4',
  lg: 'px-6 py-6',
};

@Component({
  selector: 'mnl-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block',
  },
  template: `
    <article
      [attr.data-interactive]="interactive() ? 'true' : 'false'"
      [attr.data-padding]="padding()"
      [class]="cardClasses()"
      data-testid="mnl-card"
    >
      <div [class]="headerClasses()" data-testid="mnl-card-header">
        <ng-content select="[mnlCardHeader]"></ng-content>
      </div>

      <div [class]="bodyClasses()" data-testid="mnl-card-body">
        <ng-content></ng-content>
      </div>

      <div [class]="footerClasses()" data-testid="mnl-card-footer">
        <ng-content select="[mnlCardFooter]"></ng-content>
      </div>
    </article>
  `,
})
export class MnlCardComponent {
  readonly interactive = input(false);
  readonly padding = input<MnlCardPadding>('md');

  protected readonly bodyClasses = computed(() =>
    ['min-w-0 flex-1', sectionPaddingClasses[this.padding()]].join(' '),
  );
  protected readonly cardClasses = computed(() =>
    [cardBaseClasses, this.interactive() ? interactiveClasses : ''].filter(Boolean).join(' '),
  );
  protected readonly footerClasses = computed(() =>
    [
      'empty:hidden min-w-0 border-t border-mnl-border/70',
      sectionPaddingClasses[this.padding()],
    ].join(' '),
  );
  protected readonly headerClasses = computed(() =>
    [
      'empty:hidden min-w-0 border-b border-mnl-border/70',
      sectionPaddingClasses[this.padding()],
    ].join(' '),
  );
}
