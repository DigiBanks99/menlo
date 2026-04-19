import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { Subject, of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ApiError, Result, failure, success, unknownError } from 'shared-util';
import { BudgetApiService, BudgetResponse } from 'data-access-menlo-api';
import { BudgetWidgetComponent } from './budget-widget.component';

const currentYear = new Date().getFullYear();

const mockBudget: BudgetResponse = {
  id: 'budget-1',
  year: currentYear,
  householdId: 'household-1',
  status: 'Active',
  categories: [],
  totalPlannedMonthlyAmount: { amount: 10000, currency: 'ZAR' },
};

describe('BudgetWidgetComponent', () => {
  let mockBudgetApiService: { createOrEnsureBudget: ReturnType<typeof vi.fn> };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockBudgetApiService = { createOrEnsureBudget: vi.fn() };
    mockRouter = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [BudgetWidgetComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: BudgetApiService, useValue: mockBudgetApiService },
        { provide: Router, useValue: mockRouter },
      ],
    }).compileComponents();
  });

  it('creates successfully', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(of(success(mockBudget)));
    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders widget title with current year', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(of(success(mockBudget)));

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    const title = fixture.nativeElement.querySelector(
      '[data-testid="widget-title"]',
    ) as HTMLElement;
    expect(title.textContent?.trim()).toBe(`Budget ${currentYear}`);
  });

  it('loads budget on init and calls createOrEnsureBudget with current year', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(of(success(mockBudget)));

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    expect(mockBudgetApiService.createOrEnsureBudget).toHaveBeenCalledWith(currentYear);
    expect(fixture.componentInstance.budget()).toEqual(mockBudget);
  });

  it('shows budget status after loading', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(of(success(mockBudget)));

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    const statusEl = fixture.nativeElement.querySelector(
      '[data-testid="widget-status"]',
    ) as HTMLElement;
    expect(statusEl).toBeTruthy();
    expect(statusEl.textContent?.trim()).toBe('Active');
  });

  it('shows budget total planned monthly amount after loading', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(of(success(mockBudget)));

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    const totalEl = fixture.nativeElement.querySelector(
      '[data-testid="widget-total"]',
    ) as HTMLElement;
    expect(totalEl).toBeTruthy();
    expect(totalEl.textContent?.trim()).toContain('10');
  });

  it('navigates to budget detail page on viewBudget click', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(of(success(mockBudget)));

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    fixture.componentInstance.viewBudget();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/budgets', 'budget-1']);
  });

  it('does not navigate when no budget has been loaded', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(
      of(failure(unknownError('Something went wrong'))),
    );

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    fixture.componentInstance.viewBudget();

    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('shows error banner when init load fails', () => {
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(
      of(failure(unknownError('Something went wrong'))),
    );

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    const errorBanner = fixture.nativeElement.querySelector(
      '[data-testid="widget-error"]',
    ) as HTMLElement;
    expect(errorBanner).toBeTruthy();
    expect(errorBanner.textContent?.trim()).toBe('Something went wrong');
    expect(mockRouter.navigate).not.toHaveBeenCalled();
  });

  it('shows loading state while request is in flight then enables button on success', () => {
    const subject = new Subject<Result<BudgetResponse, ApiError>>();
    mockBudgetApiService.createOrEnsureBudget.mockReturnValue(subject.asObservable());

    const fixture = TestBed.createComponent(BudgetWidgetComponent);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector(
      '[data-testid="view-budget-btn"]',
    ) as HTMLButtonElement;
    const loadingEl = fixture.nativeElement.querySelector(
      '[data-testid="widget-loading"]',
    ) as HTMLElement;
    expect(button.disabled).toBe(true);
    expect(loadingEl).toBeTruthy();

    subject.next(success(mockBudget));
    fixture.detectChanges();

    const loadingElAfter = fixture.nativeElement.querySelector(
      '[data-testid="widget-loading"]',
    ) as HTMLElement;
    expect(button.disabled).toBe(false);
    expect(loadingElAfter).toBeNull();
  });
});
