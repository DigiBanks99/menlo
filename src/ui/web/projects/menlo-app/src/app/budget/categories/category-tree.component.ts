import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryApiService, CategoryDto, CategoryTreeNode } from 'data-access-menlo-api';
import {
  MnlBadgeComponent,
  MnlButtonComponent,
  MnlInputComponent,
  MnlListItemComponent,
  MnlPanelComponent,
  type MnlSelectOption,
  MnlSelectComponent,
  MnlToastService,
} from 'menlo-lib';
import { ApiError, getErrorMessage, isSuccess } from 'shared-util';
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

const budgetFlowFilterOptions: readonly MnlSelectOption[] = [
  { value: 'Income', label: 'Income' },
  { value: 'Expense', label: 'Expense' },
  { value: 'Both', label: 'Both' },
];

const attributionFilterOptions: readonly MnlSelectOption[] = [
  { value: 'Main', label: 'Main' },
  { value: 'Rental', label: 'Rental' },
  { value: 'ServiceProvider', label: 'Service Provider' },
];

@Component({
  selector: 'app-category-tree',
  standalone: true,
  imports: [
    FormsModule,
    MnlBadgeComponent,
    MnlButtonComponent,
    MnlInputComponent,
    MnlListItemComponent,
    MnlPanelComponent,
    MnlSelectComponent,
    CategoryFormComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="category-tree space-y-4" data-testid="category-tree">
      <div
        class="tree-toolbar flex flex-col gap-3 rounded-2xl border border-mnl-border/70 bg-mnl-surface-alt/40 p-4 lg:flex-row lg:items-center lg:justify-between"
      >
        <div class="space-y-1">
          <h3 class="m-0 text-base font-semibold text-mnl-text">Category tree</h3>
          <p class="m-0 text-sm text-mnl-subtext">
            Create, filter, delete, and restore categories without leaving the budget detail page.
          </p>
        </div>

        <div class="flex flex-col gap-3 sm:flex-row sm:items-center">
          <mnl-button testId="btn-add-root" type="button" size="sm" (pressed)="openAddRoot()">
            Add category
          </mnl-button>

          <label
            class="toggle-deleted inline-flex items-center gap-2 rounded-xl border border-mnl-border bg-mnl-surface px-3 py-2 text-sm text-mnl-text shadow-sm"
          >
            <input
              type="checkbox"
              [ngModel]="includeDeleted()"
              (ngModelChange)="toggleIncludeDeleted()"
              data-testid="toggle-include-deleted"
            />
            <span>Show deleted</span>
          </label>
        </div>
      </div>

      <div class="tree-filters grid gap-3 md:grid-cols-[minmax(0,1fr)_repeat(2,minmax(0,14rem))]">
        <mnl-input
          placeholder="Search..."
          testId="filter-search"
          [ngModel]="searchText()"
          (ngModelChange)="searchText.set($event ?? '')"
        />

        <mnl-select
          placeholder="All Flows"
          testId="filter-budgetFlow"
          [ngModel]="filterBudgetFlow()"
          [options]="budgetFlowFilterOptions"
          (ngModelChange)="filterBudgetFlow.set($event ?? '')"
        />

        <mnl-select
          placeholder="All Attributions"
          testId="filter-attribution"
          [ngModel]="filterAttribution()"
          [options]="attributionFilterOptions"
          (ngModelChange)="filterAttribution.set($event ?? '')"
        />
      </div>

      @if (loading()) {
        <div
          class="loading rounded-2xl border border-dashed border-mnl-border px-4 py-8 text-center text-sm text-mnl-subtext"
          data-testid="tree-loading"
        >
          Loading categories...
        </div>
      }

      @if (error()) {
        <div
          class="error-banner rounded-2xl border border-mnl-error/30 bg-mnl-error/10 px-4 py-3 text-sm text-mnl-error"
          data-testid="tree-error"
        >
          {{ error() }}
        </div>
      }

      @if (!loading() && flatNodes().length === 0 && !error()) {
        <div
          class="empty rounded-2xl border border-dashed border-mnl-border px-4 py-8 text-center text-sm text-mnl-subtext"
          data-testid="tree-empty"
        >
          No categories found.
        </div>
      }

      <ul class="tree-list space-y-2" data-testid="tree-list">
        @for (node of flatNodes(); track node.id) {
          <li
            class="tree-node"
            [style.padding-left.rem]="node.depth * 1.5"
            [attr.data-testid]="'tree-node-' + node.id"
          >
            <mnl-list-item>
              <span mnlListItemLeading>
                @if (node.hasChildren) {
                  <button
                    class="btn-toggle inline-flex h-8 w-8 items-center justify-center rounded-lg border border-mnl-border bg-mnl-surface text-sm text-mnl-subtext transition-colors hover:bg-mnl-surface-alt"
                    [attr.data-testid]="'toggle-' + node.id"
                    type="button"
                    (click)="toggleExpanded(node.id)"
                  >
                    {{ isExpanded(node.id) ? '▼' : '▶' }}
                  </button>
                } @else {
                  <span class="toggle-spacer inline-flex h-8 w-8"></span>
                }
              </span>

              <div class="space-y-2">
                <div class="flex flex-wrap items-center gap-2">
                  <span
                    class="node-name text-sm font-semibold text-mnl-text"
                    [class.line-through]="node.isDeleted"
                    [class.text-mnl-subtext]="node.isDeleted"
                  >
                    {{ node.name }}
                  </span>

                  <span class="node-flow">
                    <mnl-badge size="sm" variant="neutral">{{ node.budgetFlow }}</mnl-badge>
                  </span>

                  @if (node.attribution) {
                    <mnl-badge size="sm" variant="info">{{ node.attribution }}</mnl-badge>
                  }
                </div>

                <p class="m-0 text-xs text-mnl-subtext">
                  Depth {{ node.depth }} ·
                  {{ node.hasChildren ? 'Parent category' : 'Leaf category' }}
                </p>
              </div>

              <div mnlListItemTrailing class="node-actions flex flex-wrap justify-end gap-2">
                <mnl-button
                  size="sm"
                  type="button"
                  variant="secondary"
                  [testId]="'btn-add-child-' + node.id"
                  (pressed)="openAddChild(node.id)"
                >
                  Add child
                </mnl-button>

                <mnl-button
                  size="sm"
                  type="button"
                  variant="ghost"
                  [testId]="'btn-edit-' + node.id"
                  (pressed)="openEdit(node.source)"
                >
                  Edit
                </mnl-button>

                @if (node.isDeleted) {
                  <mnl-button
                    size="sm"
                    type="button"
                    variant="secondary"
                    [testId]="'btn-restore-' + node.id"
                    (pressed)="restoreCategory(node.id)"
                  >
                    Restore
                  </mnl-button>
                } @else {
                  <mnl-button
                    size="sm"
                    type="button"
                    variant="destructive"
                    [testId]="'btn-delete-' + node.id"
                    (pressed)="deleteCategory(node.id)"
                  >
                    Delete
                  </mnl-button>
                }
              </div>
            </mnl-list-item>
          </li>
        }
      </ul>

      @if (formMode(); as mode) {
        <div data-testid="category-form-panel">
          <mnl-panel [open]="true" (closed)="closeForm()">
            <div mnlPanelHeader class="space-y-1">
              <h3 class="m-0 text-xl font-semibold text-mnl-text">{{ formTitle(mode) }}</h3>
              <p class="m-0 text-sm text-mnl-subtext">{{ formDescription(mode) }}</p>
            </div>

            <app-category-form
              [budgetId]="budgetId()"
              [category]="mode.kind === 'edit' ? editingCategory() : null"
              [parentId]="mode.kind === 'add-child' ? addChildParentId() : null"
              (saved)="onFormSaved()"
              (cancelled)="closeForm()"
            />
          </mnl-panel>
        </div>
      }
    </div>
  `,
})
export class CategoryTreeComponent {
  private readonly categoryApi = inject(CategoryApiService);
  private readonly toastService = inject(MnlToastService);

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

  protected readonly attributionFilterOptions = attributionFilterOptions;
  protected readonly budgetFlowFilterOptions = budgetFlowFilterOptions;

  readonly filteredCategories = computed(() => {
    const nodes = this.categories();
    const search = this.searchText().toLowerCase();
    const flow = this.filterBudgetFlow();
    const attribution = this.filterAttribution();

    if (!search && !flow && !attribution) {
      return nodes;
    }

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
      const budgetId = this.budgetId();
      const includeDeleted = this.includeDeleted();
      this.loadCategories(budgetId, includeDeleted);
    });
  }

  toggleIncludeDeleted(): void {
    this.includeDeleted.update((value) => !value);
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
    const category: CategoryDto = {
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

    this.formMode.set({ kind: 'edit', category });
    this.editingCategory.set(category);
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
        this.toastService.show('Category deleted.', { variant: 'success' });
        this.loadCategories(this.budgetId(), this.includeDeleted());
      } else {
        this.handleError(result.error);
      }
    });
  }

  restoreCategory(nodeId: string): void {
    this.categoryApi.restoreCategory(this.budgetId(), nodeId).subscribe((result) => {
      if (isSuccess(result)) {
        this.toastService.show('Category restored.', { variant: 'success' });
        this.loadCategories(this.budgetId(), this.includeDeleted());
      } else {
        this.handleError(result.error);
      }
    });
  }

  protected formDescription(mode: FormMode): string {
    switch (mode.kind) {
      case 'add-child':
        return 'Create a child category within the selected branch.';
      case 'edit':
        return 'Update the selected category details.';
      default:
        return 'Add a new top-level category to this budget.';
    }
  }

  protected formTitle(mode: FormMode): string {
    switch (mode.kind) {
      case 'add-child':
        return 'Add child category';
      case 'edit':
        return 'Edit category';
      default:
        return 'Add category';
    }
  }

  private handleError(error: ApiError): void {
    const message = getErrorMessage(error);
    this.error.set(message);
    this.toastService.show(message, { variant: 'error' });
  }

  private loadCategories(budgetId: string, includeDeleted: boolean): void {
    this.loading.set(true);
    this.error.set(null);

    this.categoryApi.listCategories(budgetId, includeDeleted).subscribe((result) => {
      this.loading.set(false);
      if (isSuccess(result)) {
        this.categories.set(result.value);
      } else {
        this.handleError(result.error);
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
