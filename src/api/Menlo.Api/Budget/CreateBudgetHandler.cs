using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget;

/// <summary>
/// Handler for the POST /api/budgets/{year} endpoint.
/// Creates or returns an existing budget for the authenticated user's household and given year.
/// </summary>
public static class CreateBudgetHandler
{
    public static async Task<IResult> Handle(
        int year,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IAuditStampFactory auditStampFactory,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!IsFeatureEnabled(configuration))
        {
            return Results.NotFound();
        }

        Result<UserContext, AuthError> userContextResult =
            await userContextProvider.GetUserContextAsync(cancellationToken);

        if (userContextResult.IsFailure)
        {
            return Results.Unauthorized();
        }

        UserContext userContext = userContextResult.Value;

        int currentYear = DateTimeOffset.UtcNow.Year;
        if (year != currentYear && year != currentYear + 1)
        {
            return Results.Problem(
                detail: $"Year must be {currentYear} or {currentYear + 1}.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid year");
        }

        Lib.Budget.Entities.Budget? existing = await budgetContext.Budgets
            .Include(b => b.Categories)
            .Include(b => b.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.HouseholdId == userContext.HouseholdId && b.Year == year,
                cancellationToken);

        if (existing is not null)
        {
            return Results.Ok(MapToDto(existing));
        }

        // When creating next year's budget, clone from the current year's budget if one exists
        Lib.Budget.Entities.Budget? sourceForClone = null;
        if (year == currentYear + 1)
        {
            sourceForClone = await budgetContext.Budgets
                .Include(b => b.Categories)
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.HouseholdId == userContext.HouseholdId && b.Year == currentYear,
                    cancellationToken);
        }

        Result<Lib.Budget.Entities.Budget, Lib.Budget.Errors.BudgetError> createResult = sourceForClone is not null
            ? Lib.Budget.Entities.Budget.CloneForYear(sourceForClone, year, auditStampFactory)
            : Lib.Budget.Entities.Budget.Create(userContext.HouseholdId, year, auditStampFactory);

        if (createResult.IsFailure)
        {
            return Results.Problem(
                detail: createResult.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Budget creation failed");
        }

        Lib.Budget.Entities.Budget budget = createResult.Value;
        budgetContext.Budgets.Add(budget);
        await budgetContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/budgets/{budget.Id.Value}", MapToDto(budget));
    }

    internal static BudgetDto MapToDto(Lib.Budget.Entities.Budget budget)
    {
        int currentMonth = DateTime.UtcNow.Month;
        decimal totalPlanned = budget.Items
            .Where(i => !i.IsDeleted && i.Month == currentMonth)
            .Sum(i => i.BudgetFlow == BudgetFlow.Income
                ? i.PlannedAmount.Amount
                : -i.PlannedAmount.Amount);

        return new(
            Id: budget.Id.Value,
            Year: budget.Year,
            HouseholdId: budget.HouseholdId.Value,
            Status: budget.Status.ToString(),
            Categories: budget.Categories.Select(c => new CategoryNodeDto(
                Id: c.Id.Value,
                Name: c.Name.Value,
                ParentId: c.ParentId?.Value,
                PlannedMonthlyAmount: new MoneyDto(c.PlannedMonthlyAmount.Amount, c.PlannedMonthlyAmount.Currency)))
                .ToList(),
            TotalPlannedMonthlyAmount: new MoneyDto(totalPlanned, "ZAR"));
    }

    private static bool IsFeatureEnabled(IConfiguration configuration) =>
        configuration.GetValue<bool>("Features:Budget", defaultValue: false);
}
