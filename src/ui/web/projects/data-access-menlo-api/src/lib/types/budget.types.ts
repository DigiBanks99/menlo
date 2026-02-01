/**
 * @fileoverview TypeScript types for Budget domain models.
 * 
 * These types correspond to the C# DTOs in Menlo.Lib.Budget.Models
 */

/**
 * Represents money with amount and currency
 */
export interface Money {
  amount: number;
  currency: string;
}

/**
 * Represents a budget category with hierarchical structure
 */
export interface BudgetCategory {
  id: string;
  name: string;
  description?: string;
  parentId?: string;
  plannedAmount?: Money;
  displayOrder: number;
  isRoot: boolean;
  isLeaf: boolean;
  children: BudgetCategory[];
}

/**
 * Complete budget response with categories
 */
export interface Budget {
  id: string;
  name: string;
  year: number;
  month: number;
  currency: string;
  status: string;
  categories: BudgetCategory[];
  total: Money;
  createdAt?: string;
  modifiedAt?: string;
}

/**
 * Budget summary for list view
 */
export interface BudgetSummary {
  id: string;
  name: string;
  year: number;
  month: number;
  currency: string;
  status: string;
  total: Money;
  categoryCount: number;
  createdAt?: string;
}

/**
 * Request model for creating a new budget
 */
export interface CreateBudgetRequest {
  name: string;
  year: number;
  month: number;
  currency: string;
}

/**
 * Request model for updating budget details
 */
export interface UpdateBudgetRequest {
  name: string;
}

/**
 * Request model for creating a category
 */
export interface CreateCategoryRequest {
  name: string;
  description?: string;
  parentId?: string;
  displayOrder?: number;
}

/**
 * Request model for updating a category
 */
export interface UpdateCategoryRequest {
  name: string;
  description?: string;
}

/**
 * Request model for setting planned amount on a category
 */
export interface SetPlannedAmountRequest {
  amount: number;
  currency: string;
}