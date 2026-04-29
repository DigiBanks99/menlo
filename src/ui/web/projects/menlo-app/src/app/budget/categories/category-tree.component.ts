import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryApiService, CategoryDto, CategoryTreeNode } from 'data-access-menlo-api';
import { getErrorMessage, isSuccess } from 'shared-util';
import { CategoryFormComponent } from './category-form.component';

export type FormMode =
  | { kind: 'add-root' }
  | { kind: 'add-child'; parentId: string }
  | { kind: 'edit'; category: CategoryDto };

export interface FlatNode {
  id: string;
  name: string;
  depth: number;
  hasChildren: boolean;
  isDeleted: boolean;
  budgetFlow: string;
  attribution?: string;
  source: CategoryTreeNode;
}

@Component({
  selector: 'app-category-tree',
  imports: [FormsModule, CategoryFormComponent],
  template: `
    <div class="category-tree" data-testid="category-tree">
      <div class="tree-toolbar">
        <button class="btn-primary btn-sm" (click)="openAddRoot()" data-testid="btn-add-root">
          + Add Category
        </button>
        <label class="toggle-deleted">
          <input
            type="checkbox"
            [ngModel]="includeDeleted()"
            (ngModelChange)="toggleIncludeDeleted()"
            data-testid="toggle-include-deleted"
          />
          Show deleted
        </label>
      </div>

      <div class="tree-filters">
        <input
          type="text"
          placeholder="Search..."
          [ngModel]="searchText()"
          (ngModelChange)="searchText.set($event)"
          data-testid="filter-search"
        />
        <select
          [ngModel]="filterBudgetFlow()"
          (ngModelChange)="filterBudgetFlow.set($event)"
          data-testid="filter-budgetFlow"
        >
          <option value="">All Flows</option>
          <option value="Income">Income</option>
          <option value="Expense">Expense</option>
          <option value="Both">Both</option>
        </select>
        <select
          [ngModel]="filterAttribution()"
          (ngModelChange)="filterAttribution.set($event)"
          data-testid="filter-attribution"
        >
          <option value="">All Attributions</option>
          <option value="Main">Main</option>
          <option value="Rental">Rental</option>
          <option value="ServiceProvider">ServiceProvider</option>
        </select>
      </div>

      @if (loading()) {
        <div class="loading" data-testid="tree-loading">Loading categories...</div>
      }

      @if (error()) {
        <div class="error-banner" data-testid="tree-error">{{ error() }}</div>
      }

      @if (formMode()?.kind === 'add-root') {
        <app-category-form
          [budgetId]="budgetId()"
          (saved)="onFormSaved()"
          (cancelled)="closeForm()"
        />
      }

      @if (!loading() && flatNodes().length === 0 && !error()) {
        <div class="empty" data-testid="tree-empty">No categories found.</div>
      }

      <ul class="tree-list" data-testid="tree-list">
        @for (node of flatNodes(); track node.id) {
          <li
            class="tree-node"
            [style.padding-left.rem]="node.depth * 1.5"
            [attr.data-testid]="'tree-node-' + node.id"
          >
            @if (node.hasChildren) {
              <button
                class="btn-toggle"
                (click)="toggleExpanded(node.id)"
                [attr.data-testid]="'toggle-' + node.id"
              >
                {{ isExpanded(node.id) ? '▼' : '▶' }}
              </button>
            } @else {
              <span class="toggle-spacer"></span>
            }
            <span class="node-name" [class.deleted]="node.isDeleted">{{ node.name }}</span>
            <span class="node-flow">{{ node.budgetFlow }}</span>
            <div class="node-actions">
              <button
                class="btn-outline"
                (click)="openAddChild(node.id)"
                [attr.data-testid]="'btn-add-child-' + node.id"
              >
                +
              </button>
              <button
                class="btn-outline"
                (click)="openEdit(node.source)"
                [attr.data-testid]="'btn-edit-' + node.id"
              >
                ✏️
              </button>
              @if (node.isDeleted) {
                <button
                  class="btn-success"
                  (click)="restoreCategory(node.id)"
                  [attr.data-testid]="'btn-restore-' + node.id"
                >
                  Restore
                </button>
              } @else {
                <button
                  class="btn-danger"
                  (click)="deleteCategory(node.id)"
                  [attr.data-testid]="'btn-delete-' + node.id"
                >
                  Delete
                </button>
              }
            </div>
          </li>
          @if (formMode()?.kind === 'add-child' && addChildParentId() === node.id) {
            <li [style.padding-left.rem]="(node.depth + 1) * 1.5">
              <app-category-form
                [budgetId]="budgetId()"
                [parentId]="node.id"
                (saved)="onFormSaved()"
                (cancelled)="closeForm()"
              />
            </li>
          }
          @if (formMode()?.kind === 'edit' && editingCategory()?.id === node.id) {
            <li [style.padding-left.rem]="node.depth * 1.5">
              <app-category-form
                [budgetId]="budgetId()"
                [category]="editingCategory()"
                (saved)="onFormSaved()"
                (cancelled)="closeForm()"
              />
            </li>
          }
        }
      </ul>
    </div>
  `,
  styles: [
    `
      .category-tree {
        margin-top: 1rem;
      }

      .tree-toolbar {
        display: flex;
        align-items: center;
        gap: 1rem;
        margin-bottom: 0.75rem;
      }

      .toggle-deleted {
        font-size: 0.875rem;
        display: flex;
        align-items: center;
        gap: 0.25rem;
        cursor: pointer;
      }

      .tree-filters {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 0.75rem;
      }

      .tree-filters input,
      .tree-filters select {
        padding: 0.3rem 0.5rem;
        border: 1px solid #ced4da;
        border-radius: 4px;
        font-size: 0.85rem;
      }

      .tree-filters input {
        flex: 1;
      }

      .loading,
      .empty {
        padding: 1rem;
        color: #6c757d;
        text-align: center;
      }

      .error-banner {
        padding: 0.75rem;
        background: #f8d7da;
        border: 1px solid #f5c6cb;
        border-radius: 4px;
        color: #721c24;
        margin-bottom: 0.75rem;
      }

      .tree-list {
        list-style: none;
        padding: 0;
        margin: 0;
      }

      .btn-primary {
        padding: 0.35rem 0.75rem;
        background: #007bff;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.85rem;
      }

      .btn-sm {
        padding: 0.25rem 0.5rem;
        font-size: 0.8rem;
      }

      .btn-danger {
        padding: 0.2rem 0.5rem;
        background: #dc3545;
        color: white;
        border: none;
        border-radius: 3px;
        cursor: pointer;
        font-size: 0.75rem;
      }

      .btn-outline {
        padding: 0.2rem 0.5rem;
        background: transparent;
        border: 1px solid #6c757d;
        border-radius: 3px;
        cursor: pointer;
        font-size: 0.75rem;
        color: #6c757d;
      }

      .btn-success {
        padding: 0.2rem 0.5rem;
        background: #28a745;
        color: white;
        border: none;
        border-radius: 3px;
        cursor: pointer;
        font-size: 0.75rem;
      }

      .tree-node {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.4rem 0;
        border-bottom: 1px solid #f0f0f0;
      }

      .btn-toggle {
        background: none;
        border: none;
        cursor: pointer;
        font-size: 0.75rem;
        padding: 0;
        width: 1rem;
      }

      .toggle-spacer {
        width: 1rem;
        display: inline-block;
      }

      .node-name {
        flex: 1;
        color: #333;
      }

      .node-name.deleted {
        text-decoration: line-through;
        color: #999;
      }

      .node-flow {
        font-size: 0.75rem;
        color: #6c757d;
      }

      .node-actions {
        display: flex;
        gap: 0.25rem;
      }
    `,
  ],
})
export class CategoryTreeComponent {
  private readonly categoryApi = inject(CategoryApiService);

  readonly budgetId = input.required<string>();

  readonly categories = signal<CategoryTreeNode[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly includeDeleted = signal(false);
  readonly expandedIds = signal<Set<string>>(new Set());
  readonly formMode = signal<FormMode | null>(null);
  readonly editingCategory = signal<CategoryDto | null>(null);
  readonly addChildParentId = signal<string | null>(null);

  readonly searchText = signal('');
  readonly filterBudgetFlow = signal('');
  readonly filterAttribution = signal('');

  readonly filteredCategories = computed(() => {
    const nodes = this.categories();
    const search = this.searchText().toLowerCase();
    const flow = this.filterBudgetFlow();
    const attribution = this.filterAttribution();

    if (!search && !flow && !attribution) return nodes;

    return this.filterNodes(nodes, search, flow, attribution);
  });

  readonly flatNodes = computed((): FlatNode[] => {
    const expanded = this.expandedIds();
    const result: FlatNode[] = [];

    const flatten = (nodes: CategoryTreeNode[], depth: number): void => {
      for (const node of nodes) {
        result.push({
          id: node.id,
          name: node.name,
          depth,
          hasChildren: node.children.length > 0,
          isDeleted: node.isDeleted,
          budgetFlow: node.budgetFlow,
          attribution: node.attribution,
          source: node,
        });
        if (node.children.length > 0 && expanded.has(node.id)) {
          flatten(node.children, depth + 1);
        }
      }
    };

    flatten(this.filteredCategories(), 0);
    return result;
  });

  constructor() {
    effect(() => {
      const id = this.budgetId();
      const inclDeleted = this.includeDeleted();
      if (id) {
        this.loadCategories(id, inclDeleted);
      }
    });
  }

  toggleIncludeDeleted(): void {
    this.includeDeleted.update((v) => !v);
  }

  toggleExpanded(nodeId: string): void {
    this.expandedIds.update((set) => {
      const next = new Set(set);
      if (next.has(nodeId)) {
        next.delete(nodeId);
      } else {
        next.add(nodeId);
      }
      return next;
    });
  }

  isExpanded(nodeId: string): boolean {
    return this.expandedIds().has(nodeId);
  }

  openAddRoot(): void {
    this.formMode.set({ kind: 'add-root' });
    this.editingCategory.set(null);
    this.addChildParentId.set(null);
  }

  openAddChild(parentId: string): void {
    this.formMode.set({ kind: 'add-child', parentId });
    this.editingCategory.set(null);
    this.addChildParentId.set(parentId);
  }

  openEdit(node: CategoryTreeNode): void {
    const dto: CategoryDto = {
      id: node.id,
      budgetId: this.budgetId(),
      name: node.name,
      description: node.description,
      canonicalCategoryId: node.id,
      budgetFlow: node.budgetFlow,
      attribution: node.attribution,
      incomeContributor: node.incomeContributor,
      responsiblePayer: node.responsiblePayer,
      isDeleted: node.isDeleted,
    };
    this.formMode.set({ kind: 'edit', category: dto });
    this.editingCategory.set(dto);
    this.addChildParentId.set(null);
  }

  closeForm(): void {
    this.formMode.set(null);
    this.editingCategory.set(null);
    this.addChildParentId.set(null);
  }

  onFormSaved(): void {
    this.closeForm();
    this.loadCategories(this.budgetId(), this.includeDeleted());
  }

  deleteCategory(nodeId: string): void {
    this.categoryApi.deleteCategory(this.budgetId(), nodeId).subscribe((result) => {
      if (isSuccess(result)) {
        this.loadCategories(this.budgetId(), this.includeDeleted());
      } else {
        this.error.set(getErrorMessage(result.error));
      }
    });
  }

  restoreCategory(nodeId: string): void {
    this.categoryApi.restoreCategory(this.budgetId(), nodeId).subscribe((result) => {
      if (isSuccess(result)) {
        this.loadCategories(this.budgetId(), this.includeDeleted());
      } else {
        this.error.set(getErrorMessage(result.error));
      }
    });
  }

  private loadCategories(budgetId: string, includeDeleted: boolean): void {
    this.loading.set(true);
    this.error.set(null);

    this.categoryApi.listCategories(budgetId, includeDeleted).subscribe((result) => {
      this.loading.set(false);
      if (isSuccess(result)) {
        this.categories.set(result.value);
      } else {
        this.error.set(getErrorMessage(result.error));
      }
    });
  }

  private filterNodes(
    nodes: CategoryTreeNode[],
    search: string,
    flow: string,
    attribution: string,
  ): CategoryTreeNode[] {
    const result: CategoryTreeNode[] = [];
    for (const node of nodes) {
      const filteredChildren = this.filterNodes(node.children, search, flow, attribution);
      const matchesSearch = !search || node.name.toLowerCase().includes(search);
      const matchesFlow = !flow || node.budgetFlow === flow;
      const matchesAttribution = !attribution || node.attribution === attribution;

      if ((matchesSearch && matchesFlow && matchesAttribution) || filteredChildren.length > 0) {
        result.push({ ...node, children: filteredChildren });
      }
    }
    return result;
  }
}
