using Menlo.Api.Auth.Policies;
using Menlo.Api.Budget;
using Menlo.Api.Ui.Budgets;
using Menlo.Api.Ui.Shared;
using Menlo.Ui.Utilities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Menlo.Api.Ui.Home;

internal static class HomeEndpoints
{
    public static IEndpointRouteBuilder MapHomePage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", Page);
        app.MapPost("/home/budget-widget", BudgetWidget).RequireAuthorization(MenloPolicies.CanEditBudget);
        return app;
    }

    private static RazorComponentResult<HomePage> Page(HttpContext context, TimeProvider timeProvider) =>
        Render.Page<HomePage>(context, HomePageModel.Create(timeProvider.GetLocalNow().Year));

    private static async Task<RazorComponentResult<BudgetWidget>> BudgetWidget(
        BudgetUi budgets,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        int year = timeProvider.GetLocalNow().Year;
        Outcome<BudgetDto> outcome = await budgets.CreateOrEnsureBudgetAsync(year, cancellationToken);

        BudgetWidgetModel model = outcome is { IsSuccess: true, Value: { } budget }
            ? new BudgetWidgetModel(
                year,
                new BudgetWidgetBudget(
                    budget.Id,
                    budget.Status,
                    BudgetPresentation.StatusTone(budget.Status),
                    MoneyFormat.Format(budget.TotalPlannedMonthlyAmount.Amount, budget.TotalPlannedMonthlyAmount.Currency)),
                Error: null)
            : new BudgetWidgetModel(year, Budget: null, outcome.Error);

        return Render.Fragment<BudgetWidget>(model);
    }
}
