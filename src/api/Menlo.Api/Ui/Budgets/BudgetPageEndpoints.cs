using Menlo.Api.Auth.Policies;
using Menlo.Api.Budget;
using Menlo.Api.Ui.Errors;
using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;
using Menlo.Ui.Utilities;
using Microsoft.AspNetCore.Authorization;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The budget list and budget detail pages, and creating a budget.
/// </summary>
internal static class BudgetPageEndpoints
{
    public static IEndpointRouteBuilder MapBudgetPages(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder view = app.MapGroup("/budgets").RequireAuthorization(MenloPolicies.CanViewBudget);
        view.MapGet("/", List);
        view.MapGet("/{id:guid}", Detail);

        RouteGroupBuilder edit = app.MapGroup("/budgets").RequireAuthorization(MenloPolicies.CanEditBudget);
        edit.MapPost("/{year:int}", Create);
        edit.MapCategoryForms();
        edit.MapItemForms();

        return app;
    }

    private static async Task<IResult> List(
        HttpContext context,
        BudgetUi budgets,
        IAuthorizationService authorization,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
        Render.Page<BudgetListPage>(
            context,
            await BuildListAsync(context, budgets, authorization, timeProvider, createError: null, cancellationToken));

    private static async Task<IResult> Detail(
        Guid id,
        HttpContext context,
        BudgetUi budgets,
        IAuthorizationService authorization,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        Outcome<BudgetDto> outcome = await budgets.GetBudgetAsync(id, cancellationToken);
        if (outcome is not { IsSuccess: true, Value: { } budget })
        {
            return Render.Page<ProblemPage>(
                context,
                new ProblemPageModel("We couldn't load this budget", outcome.Error ?? string.Empty, "/budgets", "Back to budgets"),
                outcome.IsNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status200OK);
        }

        DateTimeOffset now = timeProvider.GetLocalNow();
        BudgetDetailPageModel model = await BudgetDetailBuilder.BuildAsync(
            budgets,
            budget,
            BudgetDetailQuery.From(context.Request.Query, now.Month),
            await CanEditAsync(context, authorization),
            now.Year,
            cancellationToken);

        return Render.Page<BudgetDetailPage>(context, model);
    }

    private static async Task<IResult> Create(
        int year,
        HttpContext context,
        BudgetUi budgets,
        IAuthorizationService authorization,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        Outcome<BudgetDto> outcome = await budgets.CreateOrEnsureBudgetAsync(year, cancellationToken);
        if (outcome is { IsSuccess: true, Value: { } budget })
        {
            Flash.Set(context, $"Budget {year} created.");
            return Navigate.To(context, $"/budgets/{budget.Id}");
        }

        BudgetListPageModel model = await BuildListAsync(
            context, budgets, authorization, timeProvider, outcome.Error, cancellationToken);

        return Render.Page<BudgetListPage>(context, model, StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<bool> CanEditAsync(HttpContext context, IAuthorizationService authorization) =>
        (await authorization.AuthorizeAsync(context.User, MenloPolicies.CanEditBudget)).Succeeded;

    private static async Task<BudgetListPageModel> BuildListAsync(
        HttpContext context,
        BudgetUi budgets,
        IAuthorizationService authorization,
        TimeProvider timeProvider,
        string? createError,
        CancellationToken cancellationToken)
    {
        bool canEdit = await CanEditAsync(context, authorization);
        Outcome<List<BudgetListItem>> outcome = await budgets.ListBudgetsAsync(cancellationToken);

        if (outcome is not { IsSuccess: true, Value: { } list })
        {
            return new BudgetListPageModel([], NextBudgetYear: null, canEdit, outcome.Error, createError);
        }

        // Only this year and next can be created; offer the first one that doesn't exist yet
        int currentYear = timeProvider.GetLocalNow().Year;
        HashSet<int> years = [.. list.Select(budget => budget.Year)];
        int? nextYear = new[] { currentYear, currentYear + 1 }
            .Where(year => !years.Contains(year))
            .Select(year => (int?)year)
            .FirstOrDefault();

        return new BudgetListPageModel([.. list.Select(ToCard)], nextYear, canEdit, LoadError: null, createError);
    }

    private static BudgetCardModel ToCard(BudgetListItem budget)
    {
        decimal spent = budget.SpentExpenses?.Amount ?? 0m;
        decimal planned = budget.PlannedExpenses.Amount;

        // Spending with nothing planned counts as fully over budget
        decimal percent = planned <= 0m ? (spent > 0m ? 100m : 0m) : spent / planned * 100m;

        string spentText = budget.SpentExpenses is null
            ? "Nothing"
            : MoneyFormat.Format(budget.SpentExpenses.Amount, budget.SpentExpenses.Currency);

        return new BudgetCardModel(
            budget.Id,
            budget.Year,
            budget.Status,
            BudgetPresentation.StatusTone(budget.Status),
            BudgetPresentation.CategoryCount(budget.CategoryCount),
            percent,
            BudgetPresentation.SpendTone(percent),
            $"{spentText} spent of {MoneyFormat.Format(planned, budget.PlannedExpenses.Currency)} planned this month",
            $"{Math.Round(percent, MidpointRounding.AwayFromZero):0}%");
    }
}
