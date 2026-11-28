using CSharpFunctionalExtensions;
using Menlo.Api.Budget;
using Menlo.Api.Ui.Errors;
using Menlo.Api.Ui.Shared;
using Menlo.Lib.Budget;
using Menlo.Lib.Budget.Errors;
using Menlo.Ui.Components;
using System.Globalization;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The budget page: one calendar year's budget, or the offer to create it. Like the JSON endpoints, it answers 404
/// while <c>Features:Budget</c> is off, and every signed-in member may use it.
/// </summary>
internal static class BudgetsEndpoints
{
    public static IEndpointRouteBuilder MapBudgetPage(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder budgets = app.MapGroup(BudgetQuery.Path).AddEndpointFilter(OnlyWithTheBudget);

        budgets.MapGet(string.Empty, Page);
        budgets.MapPost(string.Empty, Create);

        return app;
    }

    private static async ValueTask<object?> OnlyWithTheBudget(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;
        if (http.RequestServices.GetRequiredService<IConfiguration>().GetValue(PageContext.BudgetFeatureFlag, defaultValue: false))
        {
            return await next(context);
        }

        return HttpMethods.IsGet(http.Request.Method)
            ? Render.Page<NotFoundPage>(http, statusCode: StatusCodes.Status404NotFound)
            : Results.NotFound();
    }

    private static async Task<IResult> Page(
        HttpContext context,
        BudgetHandlers handlers,
        TimeProvider time,
        CancellationToken cancellationToken)
    {
        DateOnly currentMonth = BudgetCalendar.CurrentMonth(time.GetUtcNow());
        BudgetQuery query = BudgetQuery.From(context.Request, currentMonth.Year);

        BudgetDto? budget = await handlers.GetAsync(query.Year, cancellationToken);
        IReadOnlyList<int> years = await handlers.YearsAsync(cancellationToken);

        return Render.Page<BudgetPage>(context, BudgetPageModel.Create(query, budget is not null, years, currentMonth));
    }

    private static async Task<IResult> Create(HttpContext context, BudgetHandlers handlers, CancellationToken cancellationToken)
    {
        IFormCollection form = await context.Request.ReadFormAsync(cancellationToken);
        if (!int.TryParse(form["year"], NumberStyles.None, CultureInfo.InvariantCulture, out int year))
        {
            return new HxResponse().Toast("Choose a year for the budget.", MnlTone.Error).NoContent(context);
        }

        Result<BudgetDto, BudgetError> created = await handlers.CreateAsync(year, cancellationToken);
        string url = new BudgetQuery(year).Url();

        if (created.IsSuccess)
        {
            Flash.Set(context, $"The {year} budget is ready.");
            return Navigate.To(context, url);
        }

        // Someone else made it first: show them the year's budget
        if (created.Error is BudgetYearTakenError)
        {
            Flash.Set(context, created.Error.Message, MnlTone.Info);
            return Navigate.To(context, url);
        }

        return new HxResponse().Toast(created.Error.Message, MnlTone.Error).NoContent(context);
    }
}
