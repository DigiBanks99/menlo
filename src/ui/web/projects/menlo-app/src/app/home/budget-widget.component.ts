import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BudgetApiService, BudgetResponse } from 'data-access-menlo-api';
import { MoneyPipe } from 'menlo-lib';
import { ApiError, Result, getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-widget',
  imports: [MoneyPipe],
  template: `
    <div class="budget-widget">
      <h3 data-testid="widget-title">Budget {{ currentYear }}</h3>

      @if (loading()) {
        <div class="loading" data-testid="widget-loading">Loading...</div>
      }

      @if (error()) {
        <div class="error-banner" data-testid="widget-error">{{ error() }}</div>
      }

      @if (budget(); as b) {
        <div class="budget-summary">
          <span
            class="status-badge status-{{ b.status.toLowerCase() }}"
            data-testid="widget-status"
          >
            {{ b.status }}
          </span>
          <span class="total-amount" data-testid="widget-total">
            {{ b.totalPlannedMonthlyAmount | money }}
          </span>
        </div>
      }

      <button
        class="btn-primary"
        data-testid="view-budget-btn"
        [disabled]="loading() || !budget()"
        (click)="viewBudget()"
      >
        View Budget
      </button>
    </div>
  `,
  styles: [
    `
      .budget-widget {
        padding: 1.5rem;
        background: white;
        border: 1px solid #dee2e6;
        border-radius: 10px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
      }

      .budget-widget h3 {
        margin: 0 0 1rem 0;
        color: #2c3e50;
      }

      .budget-summary {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        margin-bottom: 1rem;
      }

      .status-badge {
        padding: 0.2rem 0.6rem;
        border-radius: 10px;
        font-size: 0.8rem;
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

      .total-amount {
        font-size: 1rem;
        font-weight: 500;
        color: #28a745;
      }

      .loading {
        font-size: 0.9rem;
        color: #6c757d;
        margin-bottom: 0.75rem;
      }

      .error-banner {
        padding: 0.75rem;
        background: #f8d7da;
        border: 1px solid #f5c6cb;
        border-radius: 6px;
        color: #721c24;
        margin-bottom: 0.75rem;
        font-size: 0.9rem;
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
    `,
  ],
})
export class BudgetWidgetComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly budgetApiService = inject(BudgetApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly budget = signal<BudgetResponse | null>(null);
  readonly currentYear = new Date().getFullYear();

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
