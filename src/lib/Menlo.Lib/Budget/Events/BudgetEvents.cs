using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.Events;

/// <summary>
/// Domain event raised when a new budget is created.
/// </summary>
/// <param name="BudgetId">The ID of the created budget.</param>
/// <param name="Name">The name of the budget.</param>
/// <param name="Period">The budget period.</param>
/// <param name="Currency">The budget currency.</param>
/// <param name="Timestamp">When the budget was created.</param>
public readonly record struct BudgetCreatedEvent(
    BudgetId BudgetId,
    string Name,
    BudgetPeriod Period,
    string Currency,
    DateTimeOffset Timestamp) : IDomainEvent;

/// <summary>
/// Domain event raised when a budget is activated.
/// </summary>
/// <param name="BudgetId">The ID of the activated budget.</param>
/// <param name="Timestamp">When the budget was activated.</param>
public readonly record struct BudgetActivatedEvent(
    BudgetId BudgetId,
    DateTimeOffset Timestamp) : IDomainEvent;

/// <summary>
/// Domain event raised when a category is added to a budget.
/// </summary>
/// <param name="BudgetId">The ID of the budget.</param>
/// <param name="CategoryId">The ID of the added category.</param>
/// <param name="Name">The name of the category.</param>
/// <param name="ParentCategoryId">The parent category ID, if this is a subcategory.</param>
/// <param name="Timestamp">When the category was added.</param>
public readonly record struct CategoryAddedEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    string Name,
    BudgetCategoryId? ParentCategoryId,
    DateTimeOffset Timestamp) : IDomainEvent;

/// <summary>
/// Domain event raised when a category is renamed.
/// </summary>
/// <param name="BudgetId">The ID of the budget.</param>
/// <param name="CategoryId">The ID of the renamed category.</param>
/// <param name="OldName">The previous name of the category.</param>
/// <param name="NewName">The new name of the category.</param>
/// <param name="Timestamp">When the category was renamed.</param>
public readonly record struct CategoryRenamedEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    string OldName,
    string NewName,
    DateTimeOffset Timestamp) : IDomainEvent;

/// <summary>
/// Domain event raised when a category is removed from a budget.
/// </summary>
/// <param name="BudgetId">The ID of the budget.</param>
/// <param name="CategoryId">The ID of the removed category.</param>
/// <param name="Name">The name of the removed category.</param>
/// <param name="Timestamp">When the category was removed.</param>
public readonly record struct CategoryRemovedEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    string Name,
    DateTimeOffset Timestamp) : IDomainEvent;

/// <summary>
/// Domain event raised when a planned amount is set on a category.
/// </summary>
/// <param name="BudgetId">The ID of the budget.</param>
/// <param name="CategoryId">The ID of the category.</param>
/// <param name="Amount">The planned amount that was set.</param>
/// <param name="Timestamp">When the amount was set.</param>
public readonly record struct PlannedAmountSetEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    Money Amount,
    DateTimeOffset Timestamp) : IDomainEvent;

/// <summary>
/// Domain event raised when a planned amount is cleared from a category.
/// </summary>
/// <param name="BudgetId">The ID of the budget.</param>
/// <param name="CategoryId">The ID of the category.</param>
/// <param name="PreviousAmount">The amount that was cleared.</param>
/// <param name="Timestamp">When the amount was cleared.</param>
public readonly record struct PlannedAmountClearedEvent(
    BudgetId BudgetId,
    BudgetCategoryId CategoryId,
    Money? PreviousAmount,
    DateTimeOffset Timestamp) : IDomainEvent;
