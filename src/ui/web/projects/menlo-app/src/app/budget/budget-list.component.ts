import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { BudgetApiService, BudgetListItemResponse, BudgetResponse } from 'data-access-menlo-api';
import {
  MnlBadgeComponent,
  type MnlBadgeVariant,
  MnlButtonComponent,
  MnlCardComponent,
  MnlPageHeaderComponent,
  MnlProgressComponent,
  type MnlProgressVariant,
  MnlToastService,
  MoneyPipe,
} from 'menlo-lib';
import { ApiError, Result, getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-list',
  standalone: true,
  imports: [
    CommonModule,
    MnlBadgeComponent,
    MnlButtonComponent,
    MnlCardComponent,
    MnlPageHeaderComponent,
    MnlProgressComponent,
    MoneyPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './budget-list/budget-list.component.html',
})
export class BudgetListComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly budgetApiService = inject(BudgetApiService);
  private readonly toastService = inject(MnlToastService);

  readonly currentYear = new Date().getFullYear();
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly budgets = signal<readonly BudgetListItemResponse[]>([]);
  readonly creating = signal(false);
  readonly createError = signal<string | null>(null);

  /**
   * The next budget year to create: the current year, then next year (the only years the API
   * creates), or null once both exist.
   */
  readonly nextBudgetYear = computed<number | null>(() => {
    const years = new Set(this.budgets().map((budget) => budget.year));
    return [this.currentYear, this.currentYear + 1].find((year) => !years.has(year)) ?? null;
  });

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set(null);

    this.budgetApiService
      .listBudgets()
      .subscribe((result: Result<BudgetListItemResponse[], ApiError>) => {
        this.loading.set(false);
        if (isSuccess(result)) {
          this.budgets.set(result.value);
        } else {
          this.error.set(getErrorMessage(result.error));
        }
      });
  }

  protected createBudget(): void {
    const year = this.nextBudgetYear();
    if (year === null) {
      return;
    }

    this.creating.set(true);
    this.createError.set(null);

    this.budgetApiService
      .createOrEnsureBudget(year)
      .subscribe((result: Result<BudgetResponse, ApiError>) => {
        this.creating.set(false);
        if (isSuccess(result)) {
          this.toastService.show(`Budget ${year} created.`, { variant: 'success' });
          void this.router.navigate(['/budgets', result.value.id]);
        } else {
          const message = getErrorMessage(result.error);
          this.createError.set(message);
          this.toastService.show(message, { variant: 'error' });
        }
      });
  }

  protected openBudget(budgetId: string): void {
    void this.router.navigate(['/budgets', budgetId]);
  }

  protected spentPercentage(budget: BudgetListItemResponse): number {
    const spent = budget.spentExpenses?.amount ?? 0;
    const planned = budget.plannedExpenses.amount;
    if (planned <= 0) {
      // Unplanned spending counts as fully over budget
      return spent > 0 ? 100 : 0;
    }

    return (spent / planned) * 100;
  }

  protected progressVariantFor(spentPercentage: number): MnlProgressVariant {
    if (spentPercentage >= 95) {
      return 'error';
    }

    if (spentPercentage >= 80) {
      return 'warning';
    }

    return 'success';
  }

  protected statusVariantFor(status: BudgetListItemResponse['status']): MnlBadgeVariant {
    switch (status) {
      case 'Active':
        return 'success';
      case 'Closed':
        return 'neutral';
      default:
        return 'warning';
    }
  }
}
