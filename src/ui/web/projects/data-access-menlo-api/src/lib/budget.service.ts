/**
 * @fileoverview BudgetService for interacting with Budget API endpoints.
 *
 * Provides CRUD operations for budgets and categories with Result pattern integration.
 */

import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Result, ApiError, toResult } from '@menlo/shared-util';
import {
  Budget,
  BudgetSummary,
  CreateBudgetRequest,
  UpdateBudgetRequest,
  CreateCategoryRequest,
  UpdateCategoryRequest,
  SetPlannedAmountRequest,
} from './types/budget.types';

/**
 * Service for Budget API operations
 */
@Injectable({
  providedIn: 'root',
})
export class BudgetService {
  private readonly http = inject(HttpClient);

  // ============================================================================
  // Budget Operations
  // ============================================================================

  /**
   * Lists budgets for the current user with optional filtering
   *
   * @param year Optional year filter
   * @param status Optional status filter
   * @returns Observable<Result<BudgetSummary[], ApiError>>
   */
  getBudgets(year?: number, status?: string): Observable<Result<BudgetSummary[], ApiError>> {
    const params: Record<string, string> = {};
    
    if (year !== undefined) {
      params['year'] = year.toString();
    }
    
    if (status) {
      params['status'] = status;
    }

    const urlParams = new URLSearchParams(params).toString();
    const url = urlParams ? `/api/budgets?${urlParams}` : '/api/budgets';

    return this.http.get<BudgetSummary[]>(url).pipe(toResult());
  }

  /**
   * Gets a specific budget by ID with full details
   *
   * @param budgetId Budget ID
   * @returns Observable<Result<Budget, ApiError>>
   */
  getBudget(budgetId: string): Observable<Result<Budget, ApiError>> {
    return this.http.get<Budget>(`/api/budgets/${budgetId}`).pipe(toResult());
  }

  /**
   * Creates a new budget
   *
   * @param request Budget creation request
   * @returns Observable<Result<Budget, ApiError>>
   */
  createBudget(request: CreateBudgetRequest): Observable<Result<Budget, ApiError>> {
    return this.http.post<Budget>('/api/budgets', request).pipe(toResult());
  }

  /**
   * Updates an existing budget
   *
   * @param budgetId Budget ID
   * @param request Budget update request
   * @returns Observable<Result<Budget, ApiError>>
   */
  updateBudget(budgetId: string, request: UpdateBudgetRequest): Observable<Result<Budget, ApiError>> {
    return this.http.put<Budget>(`/api/budgets/${budgetId}`, request).pipe(toResult());
  }

  /**
   * Activates a budget (changes status from Draft to Active)
   *
   * @param budgetId Budget ID
   * @returns Observable<Result<Budget, ApiError>>
   */
  activateBudget(budgetId: string): Observable<Result<Budget, ApiError>> {
    return this.http.post<Budget>(`/api/budgets/${budgetId}/activate`, {}).pipe(toResult());
  }

  // ============================================================================
  // Category Operations
  // ============================================================================

  /**
   * Creates a new category in a budget
   *
   * @param budgetId Budget ID
   * @param request Category creation request
   * @returns Observable<Result<Budget, ApiError>>
   */
  createCategory(
    budgetId: string,
    request: CreateCategoryRequest
  ): Observable<Result<Budget, ApiError>> {
    return this.http.post<Budget>(`/api/budgets/${budgetId}/categories`, request).pipe(toResult());
  }

  /**
   * Updates an existing category
   *
   * @param budgetId Budget ID
   * @param categoryId Category ID
   * @param request Category update request
   * @returns Observable<Result<Budget, ApiError>>
   */
  updateCategory(
    budgetId: string,
    categoryId: string,
    request: UpdateCategoryRequest
  ): Observable<Result<Budget, ApiError>> {
    return this.http.put<Budget>(`/api/budgets/${budgetId}/categories/${categoryId}`, request).pipe(toResult());
  }

  /**
   * Deletes a category from a budget
   *
   * @param budgetId Budget ID
   * @param categoryId Category ID
   * @returns Observable<Result<Budget, ApiError>>
   */
  deleteCategory(budgetId: string, categoryId: string): Observable<Result<Budget, ApiError>> {
    return this.http.delete<Budget>(`/api/budgets/${budgetId}/categories/${categoryId}`).pipe(toResult());
  }

  /**
   * Sets the planned amount for a category
   *
   * @param budgetId Budget ID
   * @param categoryId Category ID
   * @param request Planned amount request
   * @returns Observable<Result<Budget, ApiError>>
   */
  setPlannedAmount(
    budgetId: string,
    categoryId: string,
    request: SetPlannedAmountRequest
  ): Observable<Result<Budget, ApiError>> {
    return this.http.put<Budget>(
      `/api/budgets/${budgetId}/categories/${categoryId}/planned-amount`,
      request
    ).pipe(toResult());
  }

  /**
   * Clears the planned amount for a category
   *
   * @param budgetId Budget ID
   * @param categoryId Category ID
   * @returns Observable<Result<Budget, ApiError>>
   */
  clearPlannedAmount(budgetId: string, categoryId: string): Observable<Result<Budget, ApiError>> {
    return this.http.delete<Budget>(
      `/api/budgets/${budgetId}/categories/${categoryId}/planned-amount`
    ).pipe(toResult());
  }
}