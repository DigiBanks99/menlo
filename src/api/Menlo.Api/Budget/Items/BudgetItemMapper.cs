using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget.Items;

internal static class BudgetItemMapper
{
    internal static BudgetItemDto MapToDto(BudgetItem item) =>
        new(
            Id: item.Id.Value,
            BudgetId: item.BudgetId.Value,
            CategoryId: item.CategoryId.Value,
            Month: item.Month,
            BudgetFlow: item.BudgetFlow.ToString(),
            PlannedAmount: item.PlannedAmount.Amount,
            PlannedCurrency: item.PlannedAmount.Currency,
            RealizedAmount: item.RealizedAmount?.Amount,
            RealizedCurrency: item.RealizedAmount?.Currency,
            SpentAmount: item.SpentAmount?.Amount,
            SpentCurrency: item.SpentAmount?.Currency,
            PayerSplit: item.PayerSplit.Allocations
                .Select(a => new PayerAllocationDto(a.UserId.Value, a.Percent))
                .ToList(),
            AttributionSplit: item.AttributionSplit.Allocations
                .Select(a => new AttributionAllocationDto(a.Attribution.ToString(), a.Percent))
                .ToList(),
            AdjustmentRuleId: item.AdjustmentRuleId,
            IsManualOverride: item.IsManualOverride);

    internal static IResult MapError(BudgetError error) => error switch
    {
        NonLeafCategoryError => Results.Problem(
            detail: error.Message, statusCode: 400, title: "Non-leaf category"),
        InvalidPayerSplitError => Results.Problem(
            detail: error.Message, statusCode: 400, title: "Invalid payer split"),
        InvalidAttributionSplitError => Results.Problem(
            detail: error.Message, statusCode: 400, title: "Invalid attribution split"),
        InvalidBudgetFlowError => Results.Problem(
            detail: error.Message, statusCode: 400, title: "Invalid budget flow"),
        DuplicateBudgetItemError => Results.Conflict(new { error.Code, error.Message }),
        InvalidMonthError => Results.Problem(
            detail: error.Message, statusCode: 400, title: "Invalid month"),
        CategoryNotFoundError => Results.NotFound(),
        BudgetItemNotFoundError => Results.NotFound(),
        _ => Results.Problem(detail: error.Message, statusCode: 400, title: "Bad request"),
    };
}
