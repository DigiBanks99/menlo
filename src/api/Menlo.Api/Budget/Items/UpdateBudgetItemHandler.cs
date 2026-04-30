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

public static class UpdateBudgetItemHandler
{
    public static async Task<IResult> Handle(
        Guid budgetId,
        Guid categoryId,
        Guid itemId,
        UpdateBudgetItemRequest request,
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

        // Parse optional amounts
        Money? plannedAmount = null;
        if (request.PlannedAmount.HasValue)
        {
            string currency = request.PlannedCurrency ?? "ZAR";
            Result<Money, Menlo.Lib.Common.Abstractions.Error> moneyResult =
                Money.Create(request.PlannedAmount.Value, currency);
            if (moneyResult.IsFailure)
            {
                return Results.Problem(
                    detail: moneyResult.Error.Message,
                    statusCode: 400,
                    title: "Invalid planned amount");
            }
            plannedAmount = moneyResult.Value;
        }

        Money? realizedAmount = null;
        if (request.RealizedAmount.HasValue)
        {
            string currency = request.RealizedCurrency ?? "ZAR";
            Result<Money, Menlo.Lib.Common.Abstractions.Error> moneyResult =
                Money.Create(request.RealizedAmount.Value, currency);
            if (moneyResult.IsFailure)
            {
                return Results.Problem(
                    detail: moneyResult.Error.Message,
                    statusCode: 400,
                    title: "Invalid realized amount");
            }
            realizedAmount = moneyResult.Value;
        }

        Money? spentAmount = null;
        if (request.SpentAmount.HasValue)
        {
            string currency = request.SpentCurrency ?? "ZAR";
            Result<Money, Menlo.Lib.Common.Abstractions.Error> moneyResult =
                Money.Create(request.SpentAmount.Value, currency);
            if (moneyResult.IsFailure)
            {
                return Results.Problem(
                    detail: moneyResult.Error.Message,
                    statusCode: 400,
                    title: "Invalid spent amount");
            }
            spentAmount = moneyResult.Value;
        }

        // Parse optional splits
        PayerSplit? payerSplit = null;
        if (request.PayerSplit is { Count: > 0 })
        {
            List<PayerAllocation> payerAllocations = request.PayerSplit
                .Select(p => new PayerAllocation(new UserId(p.UserId), p.Percent))
                .ToList();

            Result<PayerSplit, BudgetError> payerResult = PayerSplit.Create(payerAllocations);
            if (payerResult.IsFailure)
            {
                return BudgetItemMapper.MapError(payerResult.Error);
            }
            payerSplit = payerResult.Value;
        }

        AttributionSplit? attributionSplit = null;
        if (request.AttributionSplit is { Count: > 0 })
        {
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
            attributionSplit = attrResult.Value;
        }

        // Update item
        BudgetItemId budgetItemId = new(itemId);
        Result<BudgetItem, BudgetError> updateResult = budget.UpdateItem(
            budgetItemId,
            plannedAmount,
            realizedAmount,
            spentAmount,
            payerSplit,
            attributionSplit);

        if (updateResult.IsFailure)
        {
            return BudgetItemMapper.MapError(updateResult.Error);
        }

        await budgetContext.SaveChangesAsync(cancellationToken);

        BudgetItemDto responseDto = BudgetItemMapper.MapToDto(updateResult.Value);
        return Results.Ok(responseDto);
    }
}
