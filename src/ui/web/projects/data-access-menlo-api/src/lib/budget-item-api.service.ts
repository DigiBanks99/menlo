import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiError, Result, toResult } from 'shared-util';
import { API_BASE_URL } from './api-base-url.token';

export interface PayerAllocationDto {
  userId: string;
  percent: number;
}

export interface AttributionAllocationDto {
  attribution: 'Main' | 'Rental' | 'ServiceProvider';
  percent: number;
}

export interface BudgetItemDto {
  id: string;
  budgetId: string;
  categoryId: string;
  month: number;
  budgetFlow: 'Income' | 'Expense';
  plannedAmount: number;
  plannedCurrency: string;
  realizedAmount: number | null;
  realizedCurrency: string | null;
  spentAmount: number | null;
  spentCurrency: string | null;
  payerSplit: PayerAllocationDto[];
  attributionSplit: AttributionAllocationDto[];
  adjustmentRuleId: string | null;
  isManualOverride: boolean;
}

export interface CreateBudgetItemRequest {
  month: number;
  budgetFlow: 'Income' | 'Expense';
  plannedAmount: number;
  plannedCurrency: string;
  payerSplit: PayerAllocationDto[];
  attributionSplit: AttributionAllocationDto[];
  adjustmentRuleId?: string;
  isManualOverride?: boolean;
}

export interface UpdateBudgetItemRequest {
  plannedAmount?: number;
  plannedCurrency?: string;
  realizedAmount?: number;
  realizedCurrency?: string;
  spentAmount?: number;
  spentCurrency?: string;
  payerSplit?: PayerAllocationDto[];
  attributionSplit?: AttributionAllocationDto[];
}

@Injectable({ providedIn: 'root' })
export class BudgetItemApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  createItem(
    budgetId: string,
    categoryId: string,
    request: CreateBudgetItemRequest,
  ): Observable<Result<BudgetItemDto, ApiError>> {
    return this.http
      .post<BudgetItemDto>(
        `${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}/items`,
        request,
      )
      .pipe(toResult());
  }

  listItems(
    budgetId: string,
    categoryId: string,
    month?: number,
  ): Observable<Result<BudgetItemDto[], ApiError>> {
    let params = new HttpParams();
    if (month != null) {
      params = params.set('month', month.toString());
    }
    return this.http
      .get<
        BudgetItemDto[]
      >(`${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}/items`, { params })
      .pipe(toResult());
  }

  updateItem(
    budgetId: string,
    categoryId: string,
    itemId: string,
    request: UpdateBudgetItemRequest,
  ): Observable<Result<BudgetItemDto, ApiError>> {
    return this.http
      .put<BudgetItemDto>(
        `${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}/items/${itemId}`,
        request,
      )
      .pipe(toResult());
  }
}
