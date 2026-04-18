import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiError, Result, toResult } from 'shared-util';
import { BudgetResponse } from './menlo-api.client';

@Injectable({ providedIn: 'root' })
export class BudgetApiService {
  private readonly http = inject(HttpClient);

  createOrEnsureBudget(year: number): Observable<Result<BudgetResponse, ApiError>> {
    return this.http.post<BudgetResponse>(`/api/budgets/${year}`, {}).pipe(toResult());
  }

  getBudget(id: string): Observable<Result<BudgetResponse, ApiError>> {
    return this.http.get<BudgetResponse>(`/api/budgets/${id}`).pipe(toResult());
  }

  activateBudget(id: string): Observable<Result<BudgetResponse, ApiError>> {
    return this.http.post<BudgetResponse>(`/api/budgets/${id}/activate`, {}).pipe(toResult());
  }
}
