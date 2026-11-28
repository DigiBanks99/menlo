using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Application.Budget.EntityConfigurations;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Api.Budget;

/// <summary>
/// What can be done with the household's budgets. The JSON endpoints call these, and so will the budget page.
/// </summary>
public sealed class BudgetHandlers(IBudgetContext context)
{
    /// <summary>
    /// Creates the budget for a calendar year.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The new budget, or why it can't be created: the year isn't a budget year, or it already has one.</returns>
    public async Task<Result<BudgetDto, BudgetError>> CreateAsync(int year, CancellationToken cancellationToken)
    {
        Result<BudgetAggregate, BudgetError> created = BudgetAggregate.Create(year);
        if (created.IsFailure)
        {
            return created.Error;
        }

        if (await context.Budgets.AnyAsync(budget => budget.Year == year, cancellationToken))
        {
            return new BudgetYearTakenError(year);
        }

        context.Budgets.Add(created.Value);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsTheYearTaken(exception))
        {
            // Someone else created the year's budget between the check and the save
            context.Budgets.Entry(created.Value).State = EntityState.Detached;
            return new BudgetYearTakenError(year);
        }

        return ToDto(created.Value);
    }

    /// <summary>
    /// Gets the budget for a calendar year.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The year's budget with its category tree, or <see langword="null"/> when the year has none.</returns>
    public async Task<BudgetDto?> GetAsync(int year, CancellationToken cancellationToken)
    {
        BudgetAggregate? budget = await context.Budgets
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Year == year, cancellationToken);

        return budget is null ? null : ToDto(budget);
    }

    /// <summary>
    /// Adds a category to a year's budget: a top-level one, or a subcategory under a top-level one.
    /// </summary>
    /// <param name="year">The calendar year of the budget.</param>
    /// <param name="name">The category's name.</param>
    /// <param name="parentId">The top-level category it sits under, or <see langword="null"/> for a top-level one.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The new category, or why it can't be added: the year has no budget, the name is blank, too long or
    /// taken among its siblings, or the parent isn't in the budget or is itself a subcategory.</returns>
    public async Task<Result<CategoryDto, BudgetError>> AddCategoryAsync(
        int year,
        string? name,
        Guid? parentId,
        CancellationToken cancellationToken)
    {
        BudgetAggregate? budget = await context.Budgets.SingleOrDefaultAsync(b => b.Year == year, cancellationToken);
        if (budget is null)
        {
            return new BudgetNotFoundError(year);
        }

        Result<Category, BudgetError> added = budget.AddCategory(
            name,
            parentId is { } id ? new CategoryId(id) : null);
        if (added.IsFailure)
        {
            return added.Error;
        }

        await context.SaveChangesAsync(cancellationToken);

        return ToDto(added.Value, children: []);
    }

    private static BudgetDto ToDto(BudgetAggregate budget)
    {
        ILookup<CategoryId?, Category> byParent = budget.Categories.ToLookup(category => category.ParentId);

        return new BudgetDto(
            budget.Id.Value,
            budget.Year,
            ByName(byParent[null])
                .Select(category => ToDto(category, ByName(byParent[category.Id]).Select(child => ToDto(child, [])).ToList()))
                .ToList());
    }

    private static CategoryDto ToDto(Category category, IReadOnlyList<CategoryDto> children) =>
        new(category.Id.Value, category.Name, category.ParentId?.Value, children);

    private static IEnumerable<Category> ByName(IEnumerable<Category> categories) =>
        categories.OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase);

    private static bool IsTheYearTaken(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: BudgetEntityTypeConfiguration.YearIndexName
        };
}
