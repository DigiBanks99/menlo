import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiError, Result, toResult } from 'shared-util';
import { API_BASE_URL } from './api-base-url.token';
import {
  CategoryDto,
  CategoryTreeNode,
  CreateCategoryRequest,
  ReparentCategoryRequest,
  UpdateCategoryRequest,
} from './menlo-api.client';

@Injectable({ providedIn: 'root' })
export class CategoryApiService {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = inject(API_BASE_URL);

  createCategory(
    budgetId: string,
    request: CreateCategoryRequest,
  ): Observable<Result<CategoryDto, ApiError>> {
    return this.http
      .post<CategoryDto>(`${this.apiBaseUrl}/api/budgets/${budgetId}/categories`, request)
      .pipe(toResult());
  }

  listCategories(
    budgetId: string,
    includeDeleted = false,
  ): Observable<Result<CategoryTreeNode[], ApiError>> {
    let params = new HttpParams();
    if (includeDeleted) {
      params = params.set('includeDeleted', 'true');
    }
    return this.http
      .get<CategoryTreeNode[]>(`${this.apiBaseUrl}/api/budgets/${budgetId}/categories`, { params })
      .pipe(toResult());
  }

  getCategory(budgetId: string, categoryId: string): Observable<Result<CategoryDto, ApiError>> {
    return this.http
      .get<CategoryDto>(`${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}`)
      .pipe(toResult());
  }

  updateCategory(
    budgetId: string,
    categoryId: string,
    request: UpdateCategoryRequest,
  ): Observable<Result<CategoryDto, ApiError>> {
    return this.http
      .put<CategoryDto>(
        `${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}`,
        request,
      )
      .pipe(toResult());
  }

  reparentCategory(
    budgetId: string,
    categoryId: string,
    request: ReparentCategoryRequest,
  ): Observable<Result<CategoryDto, ApiError>> {
    return this.http
      .put<CategoryDto>(
        `${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}/reparent`,
        request,
      )
      .pipe(toResult());
  }

  deleteCategory(budgetId: string, categoryId: string): Observable<Result<void, ApiError>> {
    return this.http
      .delete<void>(`${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}`)
      .pipe(toResult());
  }

  restoreCategory(budgetId: string, categoryId: string): Observable<Result<CategoryDto, ApiError>> {
    return this.http
      .put<CategoryDto>(
        `${this.apiBaseUrl}/api/budgets/${budgetId}/categories/${categoryId}/restore`,
        {},
      )
      .pipe(toResult());
  }
}
