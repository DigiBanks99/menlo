import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BudgetApiService, BudgetResponse } from 'data-access-menlo-api';
import {
  MoneyPipe,
  MnlBadgeComponent,
  type MnlBadgeVariant,
  MnlButtonComponent,
  MnlCardComponent,
  MnlStatComponent,
} from 'menlo-lib';
import { ApiError, Result, getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-widget',
  imports: [MoneyPipe, MnlBadgeComponent, MnlButtonComponent, MnlCardComponent, MnlStatComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mnl-card data-testid="budget-widget-card" padding="lg">
      <div class="flex items-start justify-between gap-4" mnlCardHeader>
        <div class="space-y-2">
          <p class="m-0 text-xs font-semibold uppercase tracking-[0.18em] text-mnl-accent">
            Current budget
          </p>
          <h2
            class="m-0 text-2xl font-semibold tracking-tight text-mnl-text"
            data-testid="widget-title"
          >
            Budget {{ currentYear }}
          </h2>
        </div>

        @if (budget(); as currentBudget) {
          <mnl-badge
            [variant]="statusVariant(currentBudget.status)"
            data-testid="widget-status"
            size="sm"
          >
            {{ currentBudget.status }}
          </mnl-badge>
        } @else if (error()) {
          <mnl-badge size="sm" variant="error">Needs attention</mnl-badge>
        } @else {
          <mnl-badge size="sm" variant="info">Syncing</mnl-badge>
        }
      </div>

      <div class="space-y-4">
        @if (loading()) {
          <p class="m-0 text-sm font-medium text-mnl-subtext" data-testid="widget-loading">
            Loading...
          </p>
        }

        @if (error()) {
          <div class="space-y-2" data-testid="widget-error">
            <p class="m-0 text-sm font-semibold text-mnl-error">
              We couldn&apos;t load this year&apos;s budget.
            </p>
            <p class="m-0 text-sm leading-6 text-mnl-subtext">{{ error() }}</p>
          </div>
        } @else if (budget(); as currentBudget) {
          <div data-testid="widget-total">
            <mnl-stat
              label="Planned this month"
              [value]="currentBudget.totalPlannedMonthlyAmount | money"
            />
          </div>

          <p class="m-0 text-sm leading-6 text-mnl-subtext">
            Review status and jump straight into the household budget workspace when you are ready
            to refine categories and items.
          </p>
        } @else {
          <p class="m-0 text-sm leading-6 text-mnl-subtext">
            Preparing the latest household budget snapshot.
          </p>
        }
      </div>

      <div class="flex flex-col gap-3 sm:flex-row" mnlCardFooter>
        <mnl-button
          class="w-full sm:w-auto"
          data-testid="view-budget-button"
          size="sm"
          [disabled]="loading() || !budget()"
          (pressed)="viewBudget()"
        >
          View Budget
        </mnl-button>
      </div>
    </mnl-card>
  `,
})
export class BudgetWidgetComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly budgetApiService = inject(BudgetApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly budget = signal<BudgetResponse | null>(null);
  readonly currentYear = new Date().getFullYear();

  protected statusVariant(status: BudgetResponse['status']): MnlBadgeVariant {
    switch (status) {
      case 'Active':
        return 'success';
      case 'Closed':
        return 'neutral';
      default:
        return 'warning';
    }
  }

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set(null);

    this.budgetApiService
      .createOrEnsureBudget(this.currentYear)
      .subscribe((result: Result<BudgetResponse, ApiError>) => {
        this.loading.set(false);
        if (isSuccess(result)) {
          this.budget.set(result.value);
        } else {
          this.error.set(getErrorMessage(result.error));
        }
      });
  }

  viewBudget(): void {
    const b = this.budget();
    if (b) {
      this.router.navigate(['/budgets', b.id]);
    }
  }
}
