import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { BudgetListComponent } from './budget-list.component';

describe('BudgetListComponent', () => {
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    mockRouter = { navigate: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [BudgetListComponent],
      providers: [provideZonelessChangeDetection(), { provide: Router, useValue: mockRouter }],
    }).compileComponents();
  });

  it('should expose the seeded demo budgets', () => {
    const fixture = TestBed.createComponent(BudgetListComponent);

    expect(fixture.componentInstance.budgets()).toHaveLength(3);
    expect(fixture.componentInstance.budgets()[0]?.name).toBe('Monthly Household Budget');
  });

  it('renders the seeded budgets with design-system cards, progress bars, and badges', () => {
    const fixture = TestBed.createComponent(BudgetListComponent);
    fixture.detectChanges();

    const cards = fixture.nativeElement.querySelectorAll('[data-testid^="budget-card-"]');
    const firstProgress = fixture.nativeElement.querySelector(
      '[data-testid="budget-progress-1"] [data-testid="mnl-progress"]',
    ) as HTMLElement;
    const secondProgress = fixture.nativeElement.querySelector(
      '[data-testid="budget-progress-2"] [data-testid="mnl-progress"]',
    ) as HTMLElement;
    const firstStatus = fixture.nativeElement.querySelector(
      '[data-testid="budget-status-1"] [data-testid="mnl-badge"]',
    ) as HTMLElement;
    const secondStatus = fixture.nativeElement.querySelector(
      '[data-testid="budget-status-2"] [data-testid="mnl-badge"]',
    ) as HTMLElement;

    expect(cards).toHaveLength(3);
    expect(firstProgress.dataset.variant).toBe('success');
    expect(secondProgress.dataset.variant).toBe('warning');
    expect(firstStatus.dataset.variant).toBe('success');
    expect(secondStatus.dataset.variant).toBe('warning');
  });

  it('renders the overspend state with error variants', () => {
    const fixture = TestBed.createComponent(BudgetListComponent);
    fixture.componentInstance.budgets.set([
      {
        id: 'overspent',
        name: 'Travel Fund',
        period: 'December 2025',
        spent: 5100,
        total: 5000,
        spentPercentage: 102,
        status: 'danger',
        statusIcon: '🚨',
        statusText: 'Overspent',
      },
    ]);
    fixture.detectChanges();

    const progress = fixture.nativeElement.querySelector(
      '[data-testid="budget-progress-overspent"] [data-testid="mnl-progress"]',
    ) as HTMLElement;
    const status = fixture.nativeElement.querySelector(
      '[data-testid="budget-status-overspent"] [data-testid="mnl-badge"]',
    ) as HTMLElement;

    expect(progress.dataset.variant).toBe('error');
    expect(status.dataset.variant).toBe('error');
  });

  it('shows the empty state call-to-action when there are no budgets', () => {
    const fixture = TestBed.createComponent(BudgetListComponent);
    fixture.componentInstance.budgets.set([]);
    fixture.detectChanges();

    const emptyState = fixture.nativeElement.querySelector(
      '[data-testid="budget-list-empty-state"]',
    ) as HTMLElement;

    expect(emptyState).toBeTruthy();
    expect(emptyState.textContent).toContain('No budgets yet');
    expect(emptyState.textContent).toContain('Create Your First Budget');
  });

  it('navigates to the budget detail page when the view action is pressed', () => {
    const fixture = TestBed.createComponent(BudgetListComponent);
    fixture.detectChanges();

    const viewButton = fixture.nativeElement.querySelector(
      '[data-testid="budget-view-1"] [data-testid="mnl-button"]',
    ) as HTMLButtonElement;

    viewButton.click();

    expect(mockRouter.navigate).toHaveBeenCalledWith(['/budgets', '1']);
  });
});
