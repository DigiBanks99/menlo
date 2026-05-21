import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { BudgetItemApiService, BudgetSummary } from 'data-access-menlo-api';
import { getErrorMessage } from 'shared-util';

const MONTH_NAMES = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
];

@Component({
  selector: 'app-budget-summary',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-4">
      <div class="view-controls flex flex-wrap gap-2">
        <button
          [class]="toggleButtonClasses('yearly')"
          type="button"
          (click)="setViewMode('yearly')"
        >
          Yearly
        </button>
        <button
          [class]="toggleButtonClasses('monthly')"
          type="button"
          (click)="setViewMode('monthly')"
        >
          Monthly
        </button>
      </div>

      @if (viewMode() === 'monthly') {
        <nav
          class="month-nav flex flex-col gap-3 rounded-2xl border border-mnl-border/70 bg-mnl-surface-alt/40 p-4 sm:flex-row sm:items-center sm:justify-between"
        >
          <button
            class="inline-flex items-center justify-center rounded-xl border border-mnl-border bg-mnl-surface px-4 py-2 text-sm font-medium text-mnl-text shadow-sm transition-colors hover:bg-mnl-surface-alt disabled:cursor-not-allowed disabled:opacity-60"
            [disabled]="currentMonth() <= 1"
            type="button"
            (click)="previousMonth()"
          >
            ← Previous Month
          </button>

          <span
            class="current-month text-sm font-semibold uppercase tracking-wide text-mnl-subtext"
          >
            {{ monthLabel() }}
          </span>

          <button
            class="inline-flex items-center justify-center rounded-xl border border-mnl-border bg-mnl-surface px-4 py-2 text-sm font-medium text-mnl-text shadow-sm transition-colors hover:bg-mnl-surface-alt disabled:cursor-not-allowed disabled:opacity-60"
            [disabled]="currentMonth() >= 12"
            type="button"
            (click)="nextMonth()"
          >
            Next Month →
          </button>
        </nav>
      }

      @if (loading()) {
        <div
          class="loading rounded-2xl border border-dashed border-mnl-border px-4 py-8 text-center text-sm text-mnl-subtext"
        >
          Loading summary...
        </div>
      } @else if (error()) {
        <div
          class="error rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
        >
          {{ error() }}
        </div>
      } @else if (summary()) {
        <div class="balance-sheet space-y-6">
          <section class="income-section space-y-3">
            <h3 class="m-0 text-base font-semibold text-mnl-text">Income</h3>
            @for (cat of summary()!.income; track cat.id) {
              <button
                class="category-row root flex w-full items-center gap-3 rounded-2xl border border-mnl-border/70 bg-mnl-surface px-4 py-3 text-left transition-colors hover:bg-mnl-surface-alt"
                type="button"
                (click)="toggle(cat.id)"
              >
                <span class="expand-icon inline-flex w-4 justify-center text-mnl-subtext">
                  {{ isExpanded(cat.id) ? '▼' : '▶' }}
                </span>
                <span class="name min-w-0 flex-1 font-medium text-mnl-text">{{ cat.name }}</span>
                <span class="planned w-28 text-right font-semibold text-mnl-text">
                  {{ cat.plannedTotal | number: '1.2-2' }}
                </span>
                @if (hasRealized()) {
                  <span class="realized w-28 text-right font-semibold text-mnl-text">
                    {{ cat.realizedTotal | number: '1.2-2' }}
                  </span>
                }
                @if (hasSpent()) {
                  <span class="spent w-28 text-right font-semibold text-mnl-text">
                    {{ cat.spentTotal | number: '1.2-2' }}
                  </span>
                }
              </button>

              @if (isExpanded(cat.id)) {
                @for (child of cat.children; track child.id) {
                  <div
                    class="category-row child flex items-center gap-3 rounded-2xl border border-mnl-border/60 bg-mnl-surface-alt/40 px-4 py-3"
                  >
                    <span class="expand-icon inline-flex w-4"></span>
                    <span class="name min-w-0 flex-1 text-sm text-mnl-text">{{ child.name }}</span>
                    <span class="planned w-28 text-right text-sm font-medium text-mnl-text">
                      {{ child.plannedTotal | number: '1.2-2' }}
                    </span>
                    @if (hasRealized()) {
                      <span class="realized w-28 text-right text-sm font-medium text-mnl-text">
                        {{ child.realizedTotal | number: '1.2-2' }}
                      </span>
                    }
                    @if (hasSpent()) {
                      <span class="spent w-28 text-right text-sm font-medium text-mnl-text">
                        {{ child.spentTotal | number: '1.2-2' }}
                      </span>
                    }
                  </div>
                }
              }
            }
          </section>

          <section class="expenses-section space-y-3">
            <h3 class="m-0 text-base font-semibold text-mnl-text">Expenses</h3>
            @for (cat of summary()!.expenses; track cat.id) {
              <button
                class="category-row root flex w-full items-center gap-3 rounded-2xl border border-mnl-border/70 bg-mnl-surface px-4 py-3 text-left transition-colors hover:bg-mnl-surface-alt"
                type="button"
                (click)="toggle(cat.id)"
              >
                <span class="expand-icon inline-flex w-4 justify-center text-mnl-subtext">
                  {{ isExpanded(cat.id) ? '▼' : '▶' }}
                </span>
                <span class="name min-w-0 flex-1 font-medium text-mnl-text">{{ cat.name }}</span>
                <span class="planned w-28 text-right font-semibold text-mnl-text">
                  {{ cat.plannedTotal | number: '1.2-2' }}
                </span>
                @if (hasRealized()) {
                  <span class="realized w-28 text-right font-semibold text-mnl-text">
                    {{ cat.realizedTotal | number: '1.2-2' }}
                  </span>
                }
                @if (hasSpent()) {
                  <span class="spent w-28 text-right font-semibold text-mnl-text">
                    {{ cat.spentTotal | number: '1.2-2' }}
                  </span>
                }
              </button>

              @if (isExpanded(cat.id)) {
                @for (child of cat.children; track child.id) {
                  <div
                    class="category-row child flex items-center gap-3 rounded-2xl border border-mnl-border/60 bg-mnl-surface-alt/40 px-4 py-3"
                  >
                    <span class="expand-icon inline-flex w-4"></span>
                    <span class="name min-w-0 flex-1 text-sm text-mnl-text">{{ child.name }}</span>
                    <span class="planned w-28 text-right text-sm font-medium text-mnl-text">
                      {{ child.plannedTotal | number: '1.2-2' }}
                    </span>
                    @if (hasRealized()) {
                      <span class="realized w-28 text-right text-sm font-medium text-mnl-text">
                        {{ child.realizedTotal | number: '1.2-2' }}
                      </span>
                    }
                    @if (hasSpent()) {
                      <span class="spent w-28 text-right text-sm font-medium text-mnl-text">
                        {{ child.spentTotal | number: '1.2-2' }}
                      </span>
                    }
                  </div>
                }
              }
            }
          </section>

          <section class="net-section space-y-3">
            <h3 class="m-0 text-base font-semibold text-mnl-text">Net</h3>
            <div
              class="net-row flex items-center gap-3 rounded-2xl border border-mnl-accent/25 bg-mnl-accent/10 px-4 py-3"
            >
              <span class="name min-w-0 flex-1 font-semibold text-mnl-text"
                >Net (Income - Expenses)</span
              >
              <span class="planned w-28 text-right font-semibold text-mnl-text">
                {{ summary()!.netPlanned | number: '1.2-2' }}
              </span>
              @if (hasRealized()) {
                <span class="realized w-28 text-right font-semibold text-mnl-text">
                  {{ summary()!.netRealized | number: '1.2-2' }}
                </span>
              }
              @if (hasSpent()) {
                <span class="spent w-28 text-right font-semibold text-mnl-text">
                  {{ summary()!.netSpent | number: '1.2-2' }}
                </span>
              }
            </div>
          </section>
        </div>
      }
    </div>
  `,
})
export class BudgetSummaryComponent {
  private readonly api = inject(BudgetItemApiService);

  readonly budgetId = input.required<string>();
  readonly month = input<number | undefined>(undefined);

  readonly summary = signal<BudgetSummary | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly viewMode = signal<'yearly' | 'monthly'>('monthly');
  readonly currentMonth = signal<number>(new Date().getMonth() + 1);

  private readonly expandedIds = signal<Set<string>>(new Set());

  constructor() {
    effect(() => {
      this.budgetId();
      untracked(() => this.loadSummary());
    });
  }

  readonly monthLabel = computed(() => {
    const month = this.currentMonth();
    return MONTH_NAMES[month - 1] ?? '';
  });

  readonly hasRealized = computed(() => {
    const summary = this.summary();
    if (!summary) {
      return false;
    }

    return summary.netRealized !== null;
  });

  readonly hasSpent = computed(() => {
    const summary = this.summary();
    if (!summary) {
      return false;
    }

    return summary.netSpent !== null;
  });

  setViewMode(mode: 'yearly' | 'monthly'): void {
    this.viewMode.set(mode);
    this.loadSummary();
  }

  previousMonth(): void {
    const month = this.currentMonth();
    if (month > 1) {
      this.currentMonth.set(month - 1);
      this.loadSummary();
    }
  }

  nextMonth(): void {
    const month = this.currentMonth();
    if (month < 12) {
      this.currentMonth.set(month + 1);
      this.loadSummary();
    }
  }

  toggle(categoryId: string): void {
    const current = this.expandedIds();
    const next = new Set(current);
    if (next.has(categoryId)) {
      next.delete(categoryId);
    } else {
      next.add(categoryId);
    }

    this.expandedIds.set(next);
  }

  isExpanded(categoryId: string): boolean {
    return this.expandedIds().has(categoryId);
  }

  loadSummary(): void {
    this.loading.set(true);
    this.error.set(null);
    const monthParam = this.viewMode() === 'monthly' ? this.currentMonth() : undefined;
    this.api.getSummary(this.budgetId(), monthParam).subscribe((result) => {
      if (result.isSuccess) {
        this.summary.set(result.value);
      } else {
        this.error.set(getErrorMessage(result.error, 'Failed to load summary'));
      }
      this.loading.set(false);
    });
  }

  protected toggleButtonClasses(mode: 'yearly' | 'monthly'): string {
    const baseClasses =
      'toggle-btn inline-flex items-center justify-center rounded-xl border px-4 py-2 text-sm font-semibold transition-colors';

    if (this.viewMode() === mode) {
      return `${baseClasses} active border-mnl-text bg-mnl-text text-mnl-bg`;
    }

    return `${baseClasses} border-mnl-border bg-mnl-surface text-mnl-text hover:bg-mnl-surface-alt`;
  }
}
