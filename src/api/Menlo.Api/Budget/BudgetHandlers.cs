using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Application.Budget.EntityConfigurations;
using Menlo.Lib.Budget.Errors;
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
    /// <returns>The year's budget, or <see langword="null"/> when the year has none.</returns>
    public async Task<BudgetDto?> GetAsync(int year, CancellationToken cancellationToken)
    {
        BudgetAggregate? budget = await context.Budgets
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Year == year, cancellationToken);

        return budget is null ? null : ToDto(budget);
    }

    private static BudgetDto ToDto(BudgetAggregate budget) => new(budget.Id.Value, budget.Year);

    private static bool IsTheYearTaken(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: BudgetEntityTypeConfiguration.YearIndexName
        };
}
