import { Component, computed, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BudgetItemApiService, BudgetSummary } from 'data-access-menlo-api';

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
    <div class="view-controls">
      <button
        class="toggle-btn"
        [class.active]="viewMode() === 'yearly'"
        (click)="setViewMode('yearly')"
      >
        Yearly
      </button>
      <button
        class="toggle-btn"
        [class.active]="viewMode() === 'monthly'"
        (click)="setViewMode('monthly')"
      >
        Monthly
      </button>
    </div>

    @if (viewMode() === 'monthly') {
      <nav class="month-nav">
        <button [disabled]="currentMonth() <= 1" (click)="previousMonth()">← Previous Month</button>
        <span class="current-month">{{ monthLabel() }}</span>
        <button [disabled]="currentMonth() >= 12" (click)="nextMonth()">Next Month →</button>
      </nav>
    }

    @if (loading()) {
      <div class="loading">Loading summary...</div>
    } @else if (error()) {
      <div class="error">{{ error() }}</div>
    } @else if (summary()) {
      <div class="balance-sheet">
        <section class="income-section">
          <h3>Income</h3>
          @for (cat of summary()!.income; track cat.id) {
            <div class="category-row root" (click)="toggle(cat.id)">
              <span class="expand-icon">{{ isExpanded(cat.id) ? '▼' : '▶' }}</span>
              <span class="name">{{ cat.name }}</span>
              <span class="planned">{{ cat.plannedTotal | number: '1.2-2' }}</span>
              @if (hasRealized()) {
                <span class="realized">{{ cat.realizedTotal | number: '1.2-2' }}</span>
              }
              @if (hasSpent()) {
                <span class="spent">{{ cat.spentTotal | number: '1.2-2' }}</span>
              }
            </div>
            @if (isExpanded(cat.id)) {
              @for (child of cat.children; track child.id) {
                <div class="category-row child">
                  <span class="name">{{ child.name }}</span>
                  <span class="planned">{{ child.plannedTotal | number: '1.2-2' }}</span>
                  @if (hasRealized()) {
                    <span class="realized">{{ child.realizedTotal | number: '1.2-2' }}</span>
                  }
                  @if (hasSpent()) {
                    <span class="spent">{{ child.spentTotal | number: '1.2-2' }}</span>
                  }
                </div>
              }
            }
          }
        </section>

        <section class="expenses-section">
          <h3>Expenses</h3>
          @for (cat of summary()!.expenses; track cat.id) {
            <div class="category-row root" (click)="toggle(cat.id)">
              <span class="expand-icon">{{ isExpanded(cat.id) ? '▼' : '▶' }}</span>
              <span class="name">{{ cat.name }}</span>
              <span class="planned">{{ cat.plannedTotal | number: '1.2-2' }}</span>
              @if (hasRealized()) {
                <span class="realized">{{ cat.realizedTotal | number: '1.2-2' }}</span>
              }
              @if (hasSpent()) {
                <span class="spent">{{ cat.spentTotal | number: '1.2-2' }}</span>
              }
            </div>
            @if (isExpanded(cat.id)) {
              @for (child of cat.children; track child.id) {
                <div class="category-row child">
                  <span class="name">{{ child.name }}</span>
                  <span class="planned">{{ child.plannedTotal | number: '1.2-2' }}</span>
                  @if (hasRealized()) {
                    <span class="realized">{{ child.realizedTotal | number: '1.2-2' }}</span>
                  }
                  @if (hasSpent()) {
                    <span class="spent">{{ child.spentTotal | number: '1.2-2' }}</span>
                  }
                </div>
              }
            }
          }
        </section>

        <section class="net-section">
          <h3>Net</h3>
          <div class="net-row">
            <span class="name">Net (Income - Expenses)</span>
            <span class="planned">{{ summary()!.netPlanned | number: '1.2-2' }}</span>
            @if (hasRealized()) {
              <span class="realized">{{ summary()!.netRealized | number: '1.2-2' }}</span>
            }
            @if (hasSpent()) {
              <span class="spent">{{ summary()!.netSpent | number: '1.2-2' }}</span>
            }
          </div>
        </section>
      </div>
    }
  `,
  styles: [
    `
      .view-controls {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 1rem;
      }
      .toggle-btn {
        padding: 0.5rem 1rem;
        border: 1px solid #ccc;
        background: #f5f5f5;
        cursor: pointer;
      }
      .toggle-btn.active {
        background: #333;
        color: #fff;
        border-color: #333;
      }
      .month-nav {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1rem;
      }
      .month-nav button {
        padding: 0.25rem 0.75rem;
        cursor: pointer;
      }
      .month-nav button:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
      .current-month {
        font-weight: bold;
      }
      .balance-sheet {
        font-family: monospace;
      }
      .category-row {
        display: flex;
        gap: 1rem;
        padding: 0.25rem 0;
        cursor: pointer;
      }
      .category-row.child {
        padding-left: 2rem;
        cursor: default;
      }
      .expand-icon {
        width: 1rem;
      }
      .name {
        flex: 1;
      }
      .planned,
      .realized,
      .spent {
        width: 8rem;
        text-align: right;
      }
      .net-row {
        display: flex;
        gap: 1rem;
        padding: 0.5rem 0;
        font-weight: bold;
        border-top: 2px solid;
      }
      .loading,
      .error {
        padding: 1rem;
      }
      .error {
        color: red;
      }
      h3 {
        margin: 1rem 0 0.5rem;
      }
    `,
  ],
})
export class BudgetSummaryComponent {
  private readonly api = inject(BudgetItemApiService);

  budgetId = input.required<string>();
  month = input<number | undefined>(undefined);

  summary = signal<BudgetSummary | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  viewMode = signal<'yearly' | 'monthly'>('monthly');
  currentMonth = signal<number>(new Date().getMonth() + 1);

  private expandedIds = signal<Set<string>>(new Set());

  monthLabel = computed(() => {
    const m = this.currentMonth();
    return MONTH_NAMES[m - 1] ?? '';
  });

  hasRealized = computed(() => {
    const s = this.summary();
    if (!s) return false;
    return s.netRealized !== null;
  });

  hasSpent = computed(() => {
    const s = this.summary();
    if (!s) return false;
    return s.netSpent !== null;
  });

  setViewMode(mode: 'yearly' | 'monthly'): void {
    this.viewMode.set(mode);
    this.loadSummary();
  }

  previousMonth(): void {
    const m = this.currentMonth();
    if (m > 1) {
      this.currentMonth.set(m - 1);
      this.loadSummary();
    }
  }

  nextMonth(): void {
    const m = this.currentMonth();
    if (m < 12) {
      this.currentMonth.set(m + 1);
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
        this.error.set(result.error.detail ?? 'Failed to load summary');
      }
      this.loading.set(false);
    });
  }
}
