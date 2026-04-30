using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Auth.Abstractions;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Items;

public static class FillForwardHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        FillForwardBudgetItemRequest request,
        IUserContextProvider userContextProvider,
        IBudgetContext budgetContext,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Features:BudgetItems", defaultValue: false))
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
        BudgetId id = new(budgetId);

        Lib.Budget.Entities.Budget? budget = await budgetContext.Budgets
            .Include(b => b.Categories)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (budget is null || budget.HouseholdId != userContext.HouseholdId)
        {
            return Results.NotFound();
        }

        if (!Enum.TryParse<BudgetFlow>(request.BudgetFlow, ignoreCase: true, out BudgetFlow budgetFlow))
        {
            return Results.Problem(
                detail: $"Invalid BudgetFlow value: '{request.BudgetFlow}'. Valid values: Income, Expense.",
                statusCode: 400,
                title: "Validation error");
        }

        Result<Money, Menlo.Lib.Common.Abstractions.Error> moneyResult =
            Money.Create(request.Amount, request.Currency);
        if (moneyResult.IsFailure)
        {
            return Results.Problem(
                detail: moneyResult.Error.Message,
                statusCode: 400,
                title: "Invalid amount");
        }

        if (request.PayerSplit is null || request.PayerSplit.Count == 0)
        {
            return Results.Problem(
                detail: "At least one payer allocation is required.",
                statusCode: 400,
                title: "Invalid payer split");
        }

        List<PayerAllocation> payerAllocations = request.PayerSplit
            .Select(p => new PayerAllocation(new UserId(p.UserId), p.Percent))
            .ToList();

        Result<PayerSplit, BudgetError> payerResult = PayerSplit.Create(payerAllocations);
        if (payerResult.IsFailure)
        {
            return BudgetItemMapper.MapError(payerResult.Error);
        }

        if (request.AttributionSplit is null || request.AttributionSplit.Count == 0)
        {
            return Results.Problem(
                detail: "At least one attribution allocation is required.",
                statusCode: 400,
                title: "Invalid attribution split");
        }

        List<AttributionAllocation> attributionAllocations = [];
        foreach (AttributionAllocationDto dto in request.AttributionSplit)
        {
            if (!Enum.TryParse<Attribution>(dto.Attribution, ignoreCase: true, out Attribution attr))
            {
                return Results.Problem(
                    detail: $"Invalid Attribution value: '{dto.Attribution}'. Valid values: {string.Join(", ", Enum.GetNames<Attribution>())}",
                    statusCode: 400,
                    title: "Validation error");
            }
            attributionAllocations.Add(new AttributionAllocation(attr, dto.Percent));
        }

        Result<AttributionSplit, BudgetError> attrResult = AttributionSplit.Create(attributionAllocations);
        if (attrResult.IsFailure)
        {
            return BudgetItemMapper.MapError(attrResult.Error);
        }

        BudgetCategoryId catId = new(categoryId);
        Result<IReadOnlyList<BudgetItem>, BudgetError> fillResult = budget.FillForward(
            catId,
            request.FromMonth,
            budgetFlow,
            moneyResult.Value,
            payerResult.Value,
            attrResult.Value);

        if (fillResult.IsFailure)
        {
            return BudgetItemMapper.MapError(fillResult.Error);
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        List<BudgetItemDto> dtos = fillResult.Value.Select(BudgetItemMapper.MapToDto).ToList();
        return Results.Ok(dtos);
    }
}
