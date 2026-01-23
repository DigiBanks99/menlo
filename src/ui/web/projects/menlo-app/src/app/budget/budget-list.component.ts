import { CommonModule } from '@angular/common';
import { Component, inject, signal, OnInit } from '@angular/core';
import { BudgetService, BudgetSummary } from '@menlo/data-access-menlo-api';
import { isSuccess, isFailure } from '@menlo/shared-util';

@Component({
  selector: 'app-budget-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './budget-list/budget-list.component.html',
  styleUrl: './budget-list/budget-list.component.scss'
})
export class BudgetListComponent implements OnInit {
  private readonly budgetService = inject(BudgetService);

  budgets = signal<BudgetSummary[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadBudgets();
  }

  loadBudgets(): void {
    this.loading.set(true);
    this.error.set(null);

    this.budgetService.getBudgets().subscribe(result => {
      this.loading.set(false);
      
      if (isSuccess(result)) {
        this.budgets.set(result.value);
      } else if (isFailure(result)) {
        this.error.set('Failed to load budgets');
        this.budgets.set([]);
      }
    });
  }

  /**
   * Helper to calculate spending percentage for display
   */
  getSpentPercentage(budget: BudgetSummary): number {
    // TODO: This will need actual spent tracking when transaction management is implemented
    // For now, return a placeholder calculation
    return Math.floor(Math.random() * 90) + 10; // Random percentage for display
  }

  /**
   * Helper to get status display information
   */
  getStatusInfo(budget: BudgetSummary): { icon: string; text: string; cssClass: string } {
    const spentPercentage = this.getSpentPercentage(budget);
    
    if (spentPercentage < 70) {
      return { icon: '✅', text: 'On track', cssClass: 'good' };
    } else if (spentPercentage < 90) {
      return { icon: '⚠️', text: 'Watch spending', cssClass: 'warning' };
    } else {
      return { icon: '🚨', text: 'Over budget', cssClass: 'danger' };
    }
  }

  /**
   * Helper to format period for display
   */
  formatPeriod(budget: BudgetSummary): string {
    const date = new Date(budget.year, budget.month - 1);
    return date.toLocaleDateString('en-ZA', { year: 'numeric', month: 'long' });
  }
}
