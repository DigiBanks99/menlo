import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';

export interface WeatherForecast {
  date: string;
  temperatureC: number;
  summary: string;
}

@Injectable({ providedIn: 'root' })
export class MenloApiClient {
  private readonly http = inject(HttpClient);

  forecasts = signal<WeatherForecast[] | null>(null);

  loadWeather(): void {
    this.http.get<WeatherForecast[]>('/api/weatherforecast').subscribe({
      next: (data) => this.forecasts.set(data),
      error: () => this.forecasts.set([]),
    });
  }
}

export interface BudgetCategoryResponse {
  id: string;
  name: string;
  parentId: string | null;
}

export interface BudgetResponse {
  id: string;
  year: number;
  householdId: string;
  status: 'Draft' | 'Active' | 'Closed';
  categories: BudgetCategoryResponse[];
  totalPlannedMonthlyAmount: { amount: number; currency: string };
}

export interface CategoryDto {
  id: string;
  budgetId: string;
  name: string;
  description?: string;
  parentId?: string;
  canonicalCategoryId: string;
  budgetFlow: 'Income' | 'Expense' | 'Both';
  attribution?: 'Main' | 'Rental' | 'ServiceProvider';
  incomeContributor?: string;
  responsiblePayer?: string;
  isDeleted: boolean;
}

export interface CategoryTreeNode {
  id: string;
  name: string;
  description?: string;
  budgetFlow: 'Income' | 'Expense' | 'Both';
  attribution?: 'Main' | 'Rental' | 'ServiceProvider';
  incomeContributor?: string;
  responsiblePayer?: string;
  isDeleted: boolean;
  children: CategoryTreeNode[];
}

export interface CreateCategoryRequest {
  name: string;
  budgetFlow: 'Income' | 'Expense' | 'Both';
  parentId?: string;
  description?: string;
  attribution?: 'Main' | 'Rental' | 'ServiceProvider';
  incomeContributor?: string;
  responsiblePayer?: string;
}

export interface UpdateCategoryRequest {
  name: string;
  budgetFlow: 'Income' | 'Expense' | 'Both';
  description?: string;
  attribution?: 'Main' | 'Rental' | 'ServiceProvider';
  incomeContributor?: string;
  responsiblePayer?: string;
}

export interface ReparentCategoryRequest {
  newParentId?: string;
}
