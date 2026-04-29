import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { CategoryApiService, CategoryTreeNode } from 'data-access-menlo-api';
import { ApiError, Result, failure, success, unknownError } from 'shared-util';
import { CategoryTreeComponent } from './category-tree.component';

function makeNode(
  id: string,
  name: string,
  children: CategoryTreeNode[] = [],
  overrides: Partial<CategoryTreeNode> = {},
): CategoryTreeNode {
  return {
    id,
    name,
    budgetFlow: 'Expense',
    isDeleted: false,
    children,
    ...overrides,
  };
}

describe('CategoryTreeComponent', () => {
  let mockCategoryApi: {
    listCategories: ReturnType<typeof vi.fn>;
    deleteCategory: ReturnType<typeof vi.fn>;
    restoreCategory: ReturnType<typeof vi.fn>;
    createCategory: ReturnType<typeof vi.fn>;
    updateCategory: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    mockCategoryApi = {
      listCategories: vi.fn().mockReturnValue(of(success([]))),
      deleteCategory: vi.fn(),
      restoreCategory: vi.fn(),
      createCategory: vi.fn(),
      updateCategory: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [CategoryTreeComponent],
      providers: [
        provideZonelessChangeDetection(),
        { provide: CategoryApiService, useValue: mockCategoryApi },
      ],
    }).compileComponents();
  });

  describe('loading state', () => {
    it('shows loading indicator while fetching', () => {
      const subject = new Subject<Result<CategoryTreeNode[], ApiError>>();
      mockCategoryApi.listCategories.mockReturnValue(subject.asObservable());

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      const loadingEl = fixture.nativeElement.querySelector('[data-testid="tree-loading"]');
      expect(loadingEl).toBeTruthy();

      subject.next(success([]));
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('[data-testid="tree-loading"]')).toBeNull();
    });
  });

  describe('error state', () => {
    it('shows error banner on failure', () => {
      mockCategoryApi.listCategories.mockReturnValue(of(failure(unknownError('Load failed'))));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('[data-testid="tree-error"]');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent?.trim()).toBe('Load failed');
    });
  });

  describe('tree rendering', () => {
    it('shows empty state when no categories', () => {
      mockCategoryApi.listCategories.mockReturnValue(of(success([])));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      const emptyEl = fixture.nativeElement.querySelector('[data-testid="tree-empty"]');
      expect(emptyEl).toBeTruthy();
    });

    it('renders top-level nodes', () => {
      const nodes = [makeNode('n1', 'Node One'), makeNode('n2', 'Node Two')];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      const nodeEls = fixture.nativeElement.querySelectorAll('[data-testid^="tree-node-"]');
      expect(nodeEls.length).toBe(2);
    });

    it('shows expand toggle for nodes with children', () => {
      const nodes = [makeNode('parent', 'Parent', [makeNode('child', 'Child')])];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      const toggle = fixture.nativeElement.querySelector('[data-testid="toggle-parent"]');
      expect(toggle).toBeTruthy();
    });
  });

  describe('expand/collapse', () => {
    it('expands node to show children when toggled', () => {
      const nodes = [makeNode('parent', 'Parent', [makeNode('child', 'Child')])];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      // Initially collapsed - only parent visible
      let nodeEls = fixture.nativeElement.querySelectorAll('[data-testid^="tree-node-"]');
      expect(nodeEls.length).toBe(1);

      // Expand
      fixture.componentInstance.toggleExpanded('parent');
      fixture.detectChanges();

      nodeEls = fixture.nativeElement.querySelectorAll('[data-testid^="tree-node-"]');
      expect(nodeEls.length).toBe(2);
    });

    it('collapses node to hide children when toggled again', () => {
      const nodes = [makeNode('parent', 'Parent', [makeNode('child', 'Child')])];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      fixture.componentInstance.toggleExpanded('parent');
      fixture.detectChanges();

      fixture.componentInstance.toggleExpanded('parent');
      fixture.detectChanges();

      const nodeEls = fixture.nativeElement.querySelectorAll('[data-testid^="tree-node-"]');
      expect(nodeEls.length).toBe(1);
    });
  });

  describe('includeDeleted toggle', () => {
    it('reloads categories with includeDeleted when toggled', () => {
      mockCategoryApi.listCategories.mockReturnValue(of(success([])));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      mockCategoryApi.listCategories.mockClear();
      mockCategoryApi.listCategories.mockReturnValue(of(success([])));

      fixture.componentInstance.toggleIncludeDeleted();
      fixture.detectChanges();

      expect(mockCategoryApi.listCategories).toHaveBeenCalledWith('budget-1', true);
    });
  });

  describe('filtering', () => {
    it('filters by search text', () => {
      const nodes = [makeNode('n1', 'Groceries'), makeNode('n2', 'Electricity')];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      fixture.componentInstance.searchText.set('grocer');
      fixture.detectChanges();

      const nodeEls = fixture.nativeElement.querySelectorAll('[data-testid^="tree-node-"]');
      expect(nodeEls.length).toBe(1);
    });

    it('filters by budget flow', () => {
      const nodes = [
        makeNode('n1', 'Salary', [], { budgetFlow: 'Income' }),
        makeNode('n2', 'Rent', [], { budgetFlow: 'Expense' }),
      ];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      fixture.componentInstance.filterBudgetFlow.set('Income');
      fixture.detectChanges();

      const nodeEls = fixture.nativeElement.querySelectorAll('[data-testid^="tree-node-"]');
      expect(nodeEls.length).toBe(1);
    });

    it('filters by attribution', () => {
      const nodes = [
        makeNode('n1', 'Main Expense', [], { attribution: 'Main' }),
        makeNode('n2', 'Rental Income', [], { attribution: 'Rental' }),
      ];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      fixture.componentInstance.filterAttribution.set('Rental');
      fixture.detectChanges();

      const nodeEls = fixture.nativeElement.querySelectorAll('[data-testid^="tree-node-"]');
      expect(nodeEls.length).toBe(1);
    });
  });

  describe('actions', () => {
    it('opens add root form', () => {
      mockCategoryApi.listCategories.mockReturnValue(of(success([])));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      fixture.componentInstance.openAddRoot();
      fixture.detectChanges();

      const form = fixture.nativeElement.querySelector('[data-testid="category-form"]');
      expect(form).toBeTruthy();
    });

    it('calls delete API and reloads', () => {
      const nodes = [makeNode('n1', 'To Delete')];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));
      mockCategoryApi.deleteCategory.mockReturnValue(of(success(undefined)));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      mockCategoryApi.listCategories.mockClear();
      mockCategoryApi.listCategories.mockReturnValue(of(success([])));

      fixture.componentInstance.deleteCategory('n1');
      fixture.detectChanges();

      expect(mockCategoryApi.deleteCategory).toHaveBeenCalledWith('budget-1', 'n1');
      expect(mockCategoryApi.listCategories).toHaveBeenCalled();
    });

    it('calls restore API and reloads', () => {
      const nodes = [makeNode('n1', 'Deleted', [], { isDeleted: true })];
      mockCategoryApi.listCategories.mockReturnValue(of(success(nodes)));
      mockCategoryApi.restoreCategory.mockReturnValue(of(success({})));

      const fixture = TestBed.createComponent(CategoryTreeComponent);
      fixture.componentRef.setInput('budgetId', 'budget-1');
      fixture.detectChanges();

      mockCategoryApi.listCategories.mockClear();
      mockCategoryApi.listCategories.mockReturnValue(of(success([])));

      fixture.componentInstance.restoreCategory('n1');
      fixture.detectChanges();

      expect(mockCategoryApi.restoreCategory).toHaveBeenCalledWith('budget-1', 'n1');
      expect(mockCategoryApi.listCategories).toHaveBeenCalled();
    });
  });
});
