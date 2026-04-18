import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { BudgetApiService, BudgetResponse } from 'data-access-menlo-api';
import { ApiError, Result, getErrorMessage, isSuccess } from 'shared-util';

@Component({
  selector: 'app-budget-widget',
  imports: [],
  template: `
    <div class="budget-widget">
      <h3 data-testid="widget-title">Budget {{ currentYear }}</h3>

      @if (error()) {
        <div class="error-banner" data-testid="widget-error">{{ error() }}</div>
      }

      <button
        class="btn-primary"
        data-testid="view-budget-btn"
        [disabled]="loading()"
        (click)="viewBudget()"
      >
        {{ loading() ? 'Loading...' : 'View Budget' }}
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
export class BudgetWidgetComponent {
  private readonly router = inject(Router);
  private readonly budgetApiService = inject(BudgetApiService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly currentYear = new Date().getFullYear();

  viewBudget(): void {
    this.loading.set(true);
    this.error.set(null);

    this.budgetApiService
      .createOrEnsureBudget(this.currentYear)
      .subscribe((result: Result<BudgetResponse, ApiError>) => {
        this.loading.set(false);
        if (isSuccess(result)) {
          this.router.navigate(['/budgets', result.value.id]);
        } else {
          this.error.set(getErrorMessage(result.error));
        }
      });
  }
}
