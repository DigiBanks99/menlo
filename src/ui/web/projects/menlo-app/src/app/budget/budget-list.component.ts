import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  MnlBadgeComponent,
  type MnlBadgeVariant,
  MnlButtonComponent,
  MnlCardComponent,
  MnlPageHeaderComponent,
  MnlProgressComponent,
  type MnlProgressVariant,
} from 'menlo-lib';

type BudgetStatus = 'danger' | 'good' | 'warning';

interface BudgetListItem {
  readonly id: string;
  readonly name: string;
  readonly period: string;
  readonly spent: number;
  readonly spentPercentage: number;
  readonly status: BudgetStatus;
  readonly statusIcon: string;
  readonly statusText: string;
  readonly total: number;
}

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
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './budget-list/budget-list.component.html',
})
export class BudgetListComponent {
  private readonly router = inject(Router);

  readonly budgets = signal<readonly BudgetListItem[]>([
    {
      id: '1',
      name: 'Monthly Household Budget',
      period: 'December 2025',
      spent: 8750,
      total: 12000,
      spentPercentage: 73,
      status: 'good',
      statusIcon: '✅',
      statusText: 'On track',
    },
    {
      id: '2',
      name: 'Holiday Spending',
      period: 'December 2025',
      spent: 2100,
      total: 2500,
      spentPercentage: 84,
      status: 'warning',
      statusIcon: '⚠️',
      statusText: 'Watch spending',
    },
    {
      id: '3',
      name: 'Emergency Fund',
      period: 'December 2025',
      spent: 500,
      total: 5000,
      spentPercentage: 10,
      status: 'good',
      statusIcon: '💰',
      statusText: 'Healthy reserve',
    },
  ]);

  protected openBudget(budgetId: string): void {
    void this.router.navigate(['/budgets', budgetId]);
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

  protected statusVariantFor(status: BudgetStatus): MnlBadgeVariant {
    switch (status) {
      case 'danger':
        return 'error';
      case 'warning':
        return 'warning';
      default:
        return 'success';
    }
  }
}
