import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

export type MnlListItemButtonType = 'button' | 'submit' | 'reset';

const itemBaseClasses =
  'group flex w-full items-center gap-4 px-4 py-4 text-left transition-colors duration-200 motion-reduce:transition-none';
const itemDefaultClasses = 'border-b border-mnl-border/70 text-mnl-text';
const itemInteractiveClasses =
  'cursor-pointer hover:bg-mnl-surface-alt/80 focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-mnl-accent';
const itemSelectedClasses =
  'rounded-2xl border border-mnl-accent/25 bg-mnl-accent/10 text-mnl-text shadow-sm';

@Component({
  selector: 'mnl-list-item',
  standalone: true,
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'block',
  },
  template: `
    @if (rendersAsLink()) {
      <a
        [attr.aria-current]="selected() ? 'page' : null"
        [attr.data-interactive]="'true'"
        [attr.data-selected]="selected() ? 'true' : 'false'"
        [attr.href]="href()"
        [attr.rel]="resolvedRel()"
        [attr.target]="target()"
        [class]="itemClasses()"
        data-testid="mnl-list-item"
        (click)="pressed.emit($event)"
      >
        <ng-container *ngTemplateOutlet="itemContent"></ng-container>
      </a>
    } @else if (rendersAsButton()) {
      <button
        [attr.aria-pressed]="selected() ? 'true' : 'false'"
        [attr.data-interactive]="'true'"
        [attr.data-selected]="selected() ? 'true' : 'false'"
        [attr.type]="type()"
        [class]="itemClasses()"
        data-testid="mnl-list-item"
        (click)="pressed.emit($event)"
      >
        <ng-container *ngTemplateOutlet="itemContent"></ng-container>
      </button>
    } @else {
      <div
        [attr.data-interactive]="'false'"
        [attr.data-selected]="selected() ? 'true' : 'false'"
        [class]="itemClasses()"
        data-testid="mnl-list-item"
      >
        <ng-container *ngTemplateOutlet="itemContent"></ng-container>
      </div>
    }

    <ng-template #itemContent>
      <span
        class="inline-flex min-h-10 min-w-10 shrink-0 items-center justify-center text-mnl-subtext empty:hidden"
        data-testid="mnl-list-item-leading"
      >
        <ng-content select="[mnlListItemLeading]"></ng-content>
      </span>

      <span class="min-w-0 flex-1 space-y-1" data-testid="mnl-list-item-body">
        <ng-content></ng-content>
      </span>

      <span
        class="inline-flex shrink-0 items-center gap-3 text-mnl-subtext empty:hidden"
        data-testid="mnl-list-item-trailing"
      >
        <ng-content select="[mnlListItemTrailing]"></ng-content>
      </span>
    </ng-template>
  `,
})
export class MnlListItemComponent {
  readonly href = input<string | null>(null);
  readonly interactive = input(false);
  readonly rel = input<string | null>(null);
  readonly selected = input(false);
  readonly target = input<string | null>(null);
  readonly type = input<MnlListItemButtonType>('button');

  readonly pressed = output<MouseEvent>();

  protected readonly itemClasses = computed(() =>
    [
      itemBaseClasses,
      this.selected() ? itemSelectedClasses : itemDefaultClasses,
      this.isInteractive() ? itemInteractiveClasses : '',
    ]
      .filter(Boolean)
      .join(' '),
  );
  protected readonly rendersAsButton = computed(() => this.isInteractive() && !this.href());
  protected readonly rendersAsLink = computed(() => Boolean(this.href()));
  protected readonly resolvedRel = computed(() => {
    const rel = this.rel()?.trim();

    if (rel) {
      return rel;
    }

    return this.target() === '_blank' ? 'noopener noreferrer' : null;
  });

  private readonly isInteractive = computed(() => this.interactive() || Boolean(this.href()));
}
