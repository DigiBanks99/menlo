import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BudgetApiService, BudgetCategoryResponse, BudgetResponse } from 'data-access-menlo-api';
import {
  MnlBadgeComponent,
  type MnlBadgeVariant,
  MnlButtonComponent,
  MnlCardComponent,
  MnlListItemComponent,
  MnlPageHeaderComponent,
  MnlToastService,
  MoneyPipe,
} from 'menlo-lib';
import { ApiError, Result, getErrorMessage, isSuccess } from 'shared-util';
import { CategoryTreeComponent } from './categories/category-tree.component';
import { BudgetItemsWorkspaceComponent } from './items/budget-items-workspace.component';
import { BudgetSummaryComponent } from './summary/budget-summary.component';

@Component({
  selector: 'app-budget-detail',
  standalone: true,
  imports: [
    MoneyPipe,
    MnlBadgeComponent,
    MnlButtonComponent,
    MnlCardComponent,
    MnlListItemComponent,
    MnlPageHeaderComponent,
    CategoryTreeComponent,
    BudgetSummaryComponent,
    BudgetItemsWorkspaceComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mx-auto flex max-w-screen-2xl flex-col gap-6 px-4 py-6 sm:px-6 lg:px-8">
      @if (loading()) {
        <div
          class="loading rounded-2xl border border-dashed border-mnl-border px-4 py-10 text-center text-sm text-mnl-subtext"
          data-testid="loading"
        >
          Loading...
        </div>
      }

      @if (error()) {
        <div
          class="error-banner rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
          data-testid="error-banner"
        >
          {{ error() }}
        </div>
      }

      @if (budget(); as budget) {
        <mnl-page-header>
          <div
            mnlPageHeaderHero
            class="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between"
          >
            <div class="space-y-3">
              <p class="m-0 text-xs font-semibold uppercase tracking-[0.2em] text-white/70">
                Budget detail
              </p>
              <h1 class="m-0 text-4xl font-bold tracking-tight" data-testid="budget-year">
                {{ budget.year }} Budget
              </h1>
              <p class="m-0 max-w-2xl text-sm leading-6 text-white/80">
                Manage categories, line items, and year-over-year continuation from a single
                responsive workspace.
              </p>
            </div>

            <span data-testid="status-badge">
              <mnl-badge [variant]="statusVariantFor(budget.status)">{{ budget.status }}</mnl-badge>
            </span>
          </div>

          <mnl-card padding="lg">
            <div class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
              <div class="space-y-2">
                <p class="m-0 text-sm font-semibold uppercase tracking-wide text-mnl-subtext">
                  Total planned monthly
                </p>
                <div
                  class="text-3xl font-bold tracking-tight text-mnl-text"
                  data-testid="total-amount"
                >
                  {{ budget.totalPlannedMonthlyAmount | money }}
                </div>
                <p class="m-0 text-sm text-mnl-subtext">
                  Review category allocations below and open a leaf category to manage its items.
                </p>
              </div>

              <div class="space-y-3">
                @if (showCreateNextYear()) {
                  <mnl-button
                    testId="create-next-year-btn"
                    type="button"
                    [loading]="creatingNextYear()"
                    (pressed)="createNextYearBudget()"
                  >
                    {{ creatingNextYear() ? 'Creating...' : 'Create ' + nextYear() + ' Budget' }}
                  </mnl-button>
                }

                @if (createNextYearError()) {
                  <div
                    class="rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
                    data-testid="create-next-year-error"
                  >
                    {{ createNextYearError() }}
                  </div>
                }
              </div>
            </div>
          </mnl-card>
        </mnl-page-header>

        <div class="grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
          <div class="space-y-6">
            <mnl-card padding="lg">
              <div mnlCardHeader class="space-y-1">
                <h2 class="m-0 text-xl font-semibold text-mnl-text">Category management</h2>
                <p class="m-0 text-sm text-mnl-subtext">
                  Maintain the budget tree and choose a leaf category to work on its items.
                </p>
              </div>

              <div data-testid="category-tree-section">
                <app-category-tree [budgetId]="budget.id" />
              </div>
            </mnl-card>

            <mnl-card padding="lg">
              <div mnlCardHeader class="space-y-1">
                <h2 class="m-0 text-xl font-semibold text-mnl-text">Leaf category items</h2>
                <p class="m-0 text-sm text-mnl-subtext">
                  Select a leaf category to open its line-item workspace.
                </p>
              </div>

              <ul class="category-list space-y-3">
                @for (cat of sortedCategories(); track cat.id) {
                  <li
                    class="category-item"
                    [style.padding-left.rem]="getDepth(cat, budget.categories) * 1.5"
                    [attr.data-testid]="'category-' + cat.id"
                  >
                    <mnl-list-item>
                      <div class="space-y-1">
                        <div class="flex flex-wrap items-center gap-2">
                          <span class="category-name text-sm font-semibold text-mnl-text">
                            {{ cat.name }}
                          </span>
                          @if (getDepth(cat, budget.categories) >= 4) {
                            <span
                              class="depth-warning inline-flex items-center rounded-full bg-mnl-warning/20 px-2 py-0.5 text-xs font-semibold text-mnl-warning"
                              title="Consider flattening this category structure"
                              data-testid="depth-warning"
                            >
                              Deep branch
                            </span>
                          }
                        </div>
                        <p class="m-0 text-xs text-mnl-subtext">
                          Depth {{ getDepth(cat, budget.categories) }} ·
                          {{
                            isLeafCategory(cat.id, budget.categories)
                              ? 'Leaf category'
                              : 'Parent category'
                          }}
                        </p>
                      </div>

                      <div mnlListItemTrailing>
                        @if (isLeafCategory(cat.id, budget.categories)) {
                          <mnl-button
                            size="sm"
                            type="button"
                            [testId]="'btn-view-items-' + cat.id"
                            [variant]="selectedCategoryId() === cat.id ? 'secondary' : 'ghost'"
                            (pressed)="selectCategory(cat.id, cat.name)"
                          >
                            {{ selectedCategoryId() === cat.id ? 'Close items' : 'View items' }}
                          </mnl-button>
                        }
                      </div>
                    </mnl-list-item>
                  </li>
                }
              </ul>
            </mnl-card>

            @if (selectedCategoryId()) {
              <section class="items-workspace" data-testid="items-workspace-section">
                <app-budget-items-workspace
                  [budgetId]="budget.id"
                  [categoryId]="selectedCategoryId()!"
                  [categoryName]="selectedCategoryName()"
                  data-testid="items-workspace"
                />
              </section>
            }
          </div>

          <section class="summary" data-testid="budget-summary-section">
            <mnl-card padding="lg">
              <div mnlCardHeader class="space-y-1">
                <h2 class="m-0 text-xl font-semibold text-mnl-text">Summary</h2>
                <p class="m-0 text-sm text-mnl-subtext">
                  Explore yearly or monthly rollups for the selected budget.
                </p>
              </div>

              <app-budget-summary [budgetId]="budget.id" data-testid="budget-summary" />
            </mnl-card>
          </section>
        </div>
      }
    </div>
  `,
})
export class BudgetDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly budgetApiService = inject(BudgetApiService);
  private readonly toastService = inject(MnlToastService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly budget = signal<BudgetResponse | null>(null);
  readonly creatingNextYear = signal(false);
  readonly createNextYearError = signal<string | null>(null);
  readonly selectedCategoryId = signal<string | null>(null);
  readonly selectedCategoryName = signal('');

  readonly currentYear = computed(() => new Date().getFullYear());
  readonly nextYear = computed(() => this.currentYear() + 1);
  readonly showCreateNextYear = computed(() => this.budget()?.year === this.currentYear());

  readonly sortedCategories = computed(() => {
    const budget = this.budget();
    if (!budget) {
      return [];
    }

    return this.topologicalSort(budget.categories);
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
          this.toastService.show(`Budget ${this.nextYear()} created.`, { variant: 'success' });
          this.router.navigate(['/budgets', result.value.id]);
        } else {
          const message = getErrorMessage(result.error);
          this.createNextYearError.set(message);
          this.toastService.show(message, { variant: 'error' });
        }
      });
  }

  selectCategory(id: string, name: string): void {
    const budget = this.budget();
    if (!budget || !this.isLeafCategory(id, budget.categories)) {
      return;
    }

    if (this.selectedCategoryId() === id) {
      this.selectedCategoryId.set(null);
      this.selectedCategoryName.set('');
      return;
    }

    this.selectedCategoryId.set(id);
    this.selectedCategoryName.set(name);
  }

  isLeafCategory(categoryId: string, categories: BudgetCategoryResponse[]): boolean {
    return !categories.some((category) => category.parentId === categoryId);
  }

  getDepth(category: BudgetCategoryResponse, categories: BudgetCategoryResponse[]): number {
    let depth = 0;
    let current = category;

    while (current.parentId) {
      const parent = categories.find((candidate) => candidate.id === current.parentId);
      if (!parent) {
        break;
      }

      current = parent;
      depth++;
    }

    return depth;
  }

  protected statusVariantFor(status: BudgetResponse['status']): MnlBadgeVariant {
    switch (status) {
      case 'Active':
        return 'success';
      case 'Closed':
        return 'error';
      default:
        return 'neutral';
    }
  }

  private topologicalSort(categories: BudgetCategoryResponse[]): BudgetCategoryResponse[] {
    const result: BudgetCategoryResponse[] = [];
    const visited = new Set<string>();

    const visit = (category: BudgetCategoryResponse): void => {
      if (visited.has(category.id)) {
        return;
      }

      visited.add(category.id);
      result.push(category);
      categories.filter((candidate) => candidate.parentId === category.id).forEach(visit);
    };

    categories.filter((candidate) => !candidate.parentId).forEach(visit);
    categories
      .filter((candidate) => !visited.has(candidate.id))
      .forEach((candidate) => result.push(candidate));

    return result;
  }
}
