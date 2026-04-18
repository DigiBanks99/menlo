import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { isFailure, isSuccess } from 'shared-util';
import { BudgetApiService } from './budget-api.service';
import { BudgetResponse } from './menlo-api.client';

const mockBudget: BudgetResponse = {
  id: 'budget-1',
  year: 2025,
  householdId: 'household-1',
  status: 'Active',
  categories: [],
  totalPlannedMonthlyAmount: { amount: 10000, currency: 'ZAR' },
};

describe('BudgetApiService', () => {
  let service: BudgetApiService;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(BudgetApiService);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  describe('createOrEnsureBudget', () => {
    it('makes POST to /api/budgets/{year} and wraps response in success', () => {
      let result: ReturnType<typeof isSuccess> | undefined;

      service.createOrEnsureBudget(2025).subscribe((r) => {
        result = isSuccess(r);
      });

      const req = httpController.expectOne('/api/budgets/2025');
      expect(req.request.method).toBe('POST');
      req.flush(mockBudget);

      expect(result).toBe(true);
    });
  });

  describe('getBudget', () => {
    it('makes GET to /api/budgets/{id} and wraps response in success', () => {
      let resultValue: BudgetResponse | undefined;

      service.getBudget('budget-1').subscribe((r) => {
        if (isSuccess(r)) {
          resultValue = r.value;
        }
      });

      const req = httpController.expectOne('/api/budgets/budget-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockBudget);

      expect(resultValue).toEqual(mockBudget);
    });

    it('wraps 404 response in failure', () => {
      let failed = false;

      service.getBudget('nonexistent').subscribe((r) => {
        failed = isFailure(r);
      });

      const req = httpController.expectOne('/api/budgets/nonexistent');
      req.flush({ title: 'Not Found', status: 404 }, { status: 404, statusText: 'Not Found' });

      expect(failed).toBe(true);
    });
  });
});
