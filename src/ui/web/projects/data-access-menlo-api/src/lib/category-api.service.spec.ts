import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { isFailure, isSuccess } from 'shared-util';
import { CategoryApiService } from './category-api.service';
import {
  CategoryDto,
  CategoryTreeNode,
  CreateCategoryRequest,
  ReparentCategoryRequest,
  UpdateCategoryRequest,
} from './menlo-api.client';

const mockCategory: CategoryDto = {
  id: 'cat-1',
  budgetId: 'budget-1',
  name: 'Groceries',
  description: 'Food and household items',
  canonicalCategoryId: 'canonical-1',
  budgetFlow: 'Expense',
  attribution: 'Main',
  isDeleted: false,
};

const mockTree: CategoryTreeNode[] = [
  {
    id: 'cat-1',
    name: 'Groceries',
    budgetFlow: 'Expense',
    attribution: 'Main',
    isDeleted: false,
    children: [],
  },
];

describe('CategoryApiService', () => {
  let service: CategoryApiService;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(CategoryApiService);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  describe('createCategory', () => {
    it('makes POST to /api/budgets/{budgetId}/categories and wraps response in success', () => {
      const request: CreateCategoryRequest = { name: 'Groceries', budgetFlow: 'Expense' };
      let resultValue: CategoryDto | undefined;

      service.createCategory('budget-1', request).subscribe((r) => {
        if (isSuccess(r)) {
          resultValue = r.value;
        }
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockCategory);

      expect(resultValue).toEqual(mockCategory);
    });
  });

  describe('listCategories', () => {
    it('makes GET to /api/budgets/{budgetId}/categories and wraps response in success', () => {
      let resultValue: CategoryTreeNode[] | undefined;

      service.listCategories('budget-1').subscribe((r) => {
        if (isSuccess(r)) {
          resultValue = r.value;
        }
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories');
      expect(req.request.method).toBe('GET');
      req.flush(mockTree);

      expect(resultValue).toEqual(mockTree);
    });

    it('passes includeDeleted query param when true', () => {
      service.listCategories('budget-1', true).subscribe();

      const req = httpController.expectOne('/api/budgets/budget-1/categories?includeDeleted=true');
      expect(req.request.method).toBe('GET');
      req.flush(mockTree);
    });
  });

  describe('getCategory', () => {
    it('makes GET to /api/budgets/{budgetId}/categories/{categoryId}', () => {
      let resultValue: CategoryDto | undefined;

      service.getCategory('budget-1', 'cat-1').subscribe((r) => {
        if (isSuccess(r)) {
          resultValue = r.value;
        }
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories/cat-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockCategory);

      expect(resultValue).toEqual(mockCategory);
    });

    it('wraps 404 response in failure', () => {
      let failed = false;

      service.getCategory('budget-1', 'nonexistent').subscribe((r) => {
        failed = isFailure(r);
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories/nonexistent');
      req.flush({ title: 'Not Found', status: 404 }, { status: 404, statusText: 'Not Found' });

      expect(failed).toBe(true);
    });
  });

  describe('updateCategory', () => {
    it('makes PUT to /api/budgets/{budgetId}/categories/{categoryId}', () => {
      const request: UpdateCategoryRequest = { name: 'Updated', budgetFlow: 'Both' };
      let resultValue: CategoryDto | undefined;

      service.updateCategory('budget-1', 'cat-1', request).subscribe((r) => {
        if (isSuccess(r)) {
          resultValue = r.value;
        }
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories/cat-1');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(mockCategory);

      expect(resultValue).toEqual(mockCategory);
    });
  });

  describe('reparentCategory', () => {
    it('makes PUT to /api/budgets/{budgetId}/categories/{categoryId}/reparent', () => {
      const request: ReparentCategoryRequest = { newParentId: 'parent-1' };
      let resultValue: CategoryDto | undefined;

      service.reparentCategory('budget-1', 'cat-1', request).subscribe((r) => {
        if (isSuccess(r)) {
          resultValue = r.value;
        }
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories/cat-1/reparent');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(request);
      req.flush(mockCategory);

      expect(resultValue).toEqual(mockCategory);
    });
  });

  describe('deleteCategory', () => {
    it('makes DELETE to /api/budgets/{budgetId}/categories/{categoryId}', () => {
      let succeeded = false;

      service.deleteCategory('budget-1', 'cat-1').subscribe((r) => {
        succeeded = isSuccess(r);
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories/cat-1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null, { status: 204, statusText: 'No Content' });

      expect(succeeded).toBe(true);
    });
  });

  describe('restoreCategory', () => {
    it('makes PUT to /api/budgets/{budgetId}/categories/{categoryId}/restore', () => {
      let resultValue: CategoryDto | undefined;

      service.restoreCategory('budget-1', 'cat-1').subscribe((r) => {
        if (isSuccess(r)) {
          resultValue = r.value;
        }
      });

      const req = httpController.expectOne('/api/budgets/budget-1/categories/cat-1/restore');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({});
      req.flush(mockCategory);

      expect(resultValue).toEqual(mockCategory);
    });
  });
});
