import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BudgetApiService, BudgetCategoryResponse, BudgetResponse } from 'data-access-menlo-api';
import { MoneyPipe } from 'menlo-lib';
import { ApiError, Result, getErrorMessage, isSuccess } from 'shared-util';
import { CategoryTreeComponent } from './categories/category-tree.component';

@Component({
  selector: 'app-budget-detail',
  imports: [MoneyPipe, CategoryTreeComponent],
  template: `
    <div class="budget-detail">
      @if (loading()) {
        <div class="loading" data-testid="loading">Loading...</div>
      }

      @if (error()) {
        <div class="error-banner" data-testid="error-banner">{{ error() }}</div>
      }

      @if (budget(); as b) {
        <header class="budget-header">
          <h1 data-testid="budget-year">{{ b.year }} Budget</h1>
          <span class="status-badge status-{{ b.status.toLowerCase() }}" data-testid="status-badge">
            {{ b.status }}
          </span>
        </header>

        @if (showCreateNextYear()) {
          <div class="create-next-year">
            <button
              class="btn-primary"
              data-testid="create-next-year-btn"
              [disabled]="creatingNextYear()"
              (click)="createNextYearBudget()"
            >
              {{ creatingNextYear() ? 'Creating...' : 'Create ' + nextYear() + ' Budget' }}
            </button>
            @if (createNextYearError()) {
              <div class="error-banner" data-testid="create-next-year-error">
                {{ createNextYearError() }}
              </div>
            }
          </div>
        }

        <section class="categories">
          <h2>Categories</h2>
          <app-category-tree [budgetId]="b.id" data-testid="category-tree-section" />
          <ul class="category-list">
            @for (cat of sortedCategories(); track cat.id) {
              <li
                class="category-item"
                [style.padding-left.rem]="getDepth(cat, b.categories) * 1.5"
                [attr.data-testid]="'category-' + cat.id"
              >
                <span class="category-name">{{ cat.name }}</span>
                @if (getDepth(cat, b.categories) >= 4) {
                  <span
                    class="depth-warning"
                    title="Consider flattening this category structure"
                    data-testid="depth-warning"
                  >
                    ⚠️
                  </span>
                }
              </li>
            }
          </ul>
        </section>

        <footer class="budget-footer">
          <strong>Total planned monthly:</strong>
          <span class="total-amount" data-testid="total-amount">
            {{ b.totalPlannedMonthlyAmount | money }}
          </span>
        </footer>
      }
    </div>
  `,
  styles: [
    `
      .budget-detail {
        padding: 2rem;
        max-width: 900px;
        margin: 0 auto;
      }

      .budget-header {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 1.5rem;
      }

      .budget-header h1 {
        margin: 0;
        color: #2c3e50;
      }

      .status-badge {
        padding: 0.25rem 0.75rem;
        border-radius: 12px;
        font-size: 0.85rem;
        font-weight: 600;
        text-transform: uppercase;
      }

      .status-draft {
        background: #e9ecef;
        color: #495057;
      }
      .status-active {
        background: #d4edda;
        color: #155724;
      }
      .status-closed {
        background: #f8d7da;
        color: #721c24;
      }

      .loading {
        padding: 2rem;
        text-align: center;
        color: #6c757d;
      }

      .error-banner {
        padding: 1rem;
        background: #f8d7da;
        border: 1px solid #f5c6cb;
        border-radius: 6px;
        color: #721c24;
        margin-bottom: 1rem;
      }

      .create-next-year {
        margin-bottom: 1.5rem;
      }

      .btn-primary {
        padding: 0.5rem 1.25rem;
        background: #007bff;
        color: white;
        border: none;
        border-radius: 6px;
        font-size: 0.95rem;
        cursor: pointer;
        transition: background-color 0.2s;
      }

      .btn-primary:hover:not(:disabled) {
        background: #0056b3;
      }
      .btn-primary:disabled {
        opacity: 0.65;
        cursor: not-allowed;
      }

      .category-list {
        list-style: none;
        padding: 0;
        margin: 0;
      }

      .category-item {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding-top: 0.5rem;
        padding-bottom: 0.5rem;
        border-bottom: 1px solid #f0f0f0;
      }

      .category-name {
        flex: 1;
        color: #333;
      }

      .category-amount {
        font-weight: 500;
        color: #495057;
        min-width: 100px;
        text-align: right;
      }

      .depth-warning {
        font-size: 0.9rem;
        cursor: help;
      }

      .budget-footer {
        margin-top: 1.5rem;
        padding-top: 1rem;
        border-top: 2px solid #dee2e6;
        display: flex;
        gap: 0.75rem;
        font-size: 1.1rem;
      }

      .total-amount {
        color: #28a745;
        font-weight: 600;
      }
    `,
  ],
})
export class BudgetDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly budgetApiService: BudgetApiService = inject(BudgetApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly budget = signal<BudgetResponse | null>(null);
  readonly creatingNextYear = signal(false);
  readonly createNextYearError = signal<string | null>(null);

  readonly currentYear = computed(() => new Date().getFullYear());
  readonly nextYear = computed(() => this.currentYear() + 1);
  readonly showCreateNextYear = computed(() => this.budget()?.year === this.currentYear());

  readonly sortedCategories = computed(() => {
    const b = this.budget();
    if (!b) return [];
    return this.topologicalSort(b.categories);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loading.set(true);
    this.error.set(null);

    this.budgetApiService.getBudget(id).subscribe((result: Result<BudgetResponse, ApiError>) => {
      this.loading.set(false);
      if (isSuccess(result)) {
        this.budget.set(result.value);
      } else {
        this.error.set(getErrorMessage(result.error));
      }
    });
  }

  createNextYearBudget(): void {
    this.creatingNextYear.set(true);
    this.createNextYearError.set(null);

    this.budgetApiService
      .createOrEnsureBudget(this.nextYear())
      .subscribe((result: Result<BudgetResponse, ApiError>) => {
        this.creatingNextYear.set(false);
        if (isSuccess(result)) {
          this.router.navigate(['/budgets', result.value.id]);
        } else {
          this.createNextYearError.set(getErrorMessage(result.error));
        }
      });
  }

  getDepth(category: BudgetCategoryResponse, categories: BudgetCategoryResponse[]): number {
    let depth = 0;
    let current = category;
    while (current.parentId) {
      const parent = categories.find((c) => c.id === current.parentId);
      if (!parent) break;
      current = parent;
      depth++;
    }
    return depth;
  }

  private topologicalSort(categories: BudgetCategoryResponse[]): BudgetCategoryResponse[] {
    const result: BudgetCategoryResponse[] = [];
    const visited = new Set<string>();

    const visit = (cat: BudgetCategoryResponse): void => {
      if (visited.has(cat.id)) return;
      visited.add(cat.id);
      result.push(cat);
      categories.filter((c) => c.parentId === cat.id).forEach(visit);
    };

    categories.filter((c) => !c.parentId).forEach(visit);
    categories.filter((c) => !visited.has(c.id)).forEach((c) => result.push(c));

    return result;
  }
}
