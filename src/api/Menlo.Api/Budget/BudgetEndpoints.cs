using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget;

/// <summary>
/// The JSON endpoints for the household's budgets, one per calendar year. They answer 404 while
/// <c>Features:Budget</c> is off, and every signed-in member may use them.
/// </summary>
public static class BudgetEndpoints
{
    public static IHostApplicationBuilder AddMenloBudget(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<BudgetHandlers>();
        return builder;
    }

    public static RouteGroupBuilder MapBudgetEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder budgets = api
            .MapGroup("/budgets")
            .WithTags("Budgets")
            .AddEndpointFilter(OnlyWithTheBudget);

        budgets
            .MapPost("/", Create)
            .WithName("CreateBudget")
            .WithSummary("Creates the budget for a calendar year. A year has one budget.")
            .Produces<BudgetDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        budgets
            .MapGet("/{year:int}", Get)
            .WithName("GetBudget")
            .WithSummary("Gets the budget for a calendar year, with its category tree.")
            .Produces<BudgetDto>()
            .Produces(StatusCodes.Status404NotFound);

        budgets
            .MapPost("/{year:int}/categories", AddCategory)
            .WithName("AddBudgetCategory")
            .WithSummary("Adds a category to a year's budget, at the top level or under a top-level category. Categories nest at most two deep.")
            .Produces<CategoryDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return api;
    }

    private static async ValueTask<object?> OnlyWithTheBudget(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("Features:Budget", defaultValue: false)
            ? await next(context)
            : Results.NotFound();

    private static async Task<IResult> Create(
        CreateBudgetRequest request,
        BudgetHandlers handlers,
        CancellationToken cancellationToken)
    {
        Result<BudgetDto, BudgetError> created = await handlers.CreateAsync(request.Year, cancellationToken);
        if (created.IsSuccess)
        {
            return Results.Created($"/api/budgets/{created.Value.Year}", created.Value);
        }

        return created.Error is BudgetYearTakenError
            ? Problem(created.Error, "The year already has a budget", StatusCodes.Status409Conflict)
            : Problem(created.Error, "The budget can't be created", StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Get(int year, BudgetHandlers handlers, CancellationToken cancellationToken) =>
        await handlers.GetAsync(year, cancellationToken) is { } budget
            ? Results.Ok(budget)
            : Results.NotFound();

    private static async Task<IResult> AddCategory(
        int year,
        AddCategoryRequest request,
        BudgetHandlers handlers,
        CancellationToken cancellationToken)
    {
        Result<CategoryDto, BudgetError> added = await handlers.AddCategoryAsync(year, request.Name, request.ParentId, cancellationToken);
        if (added.IsSuccess)
        {
            return Results.Created($"/api/budgets/{year}/categories/{added.Value.Id}", added.Value);
        }

        return added.Error switch
        {
            BudgetNotFoundError => Problem(added.Error, "The year has no budget", StatusCodes.Status404NotFound),
            CategoryNameTakenError => Problem(added.Error, "The name is taken", StatusCodes.Status409Conflict),
            CategoryTooDeepError or CategoryNotFoundError =>
                Problem(added.Error, "The category can't go there", StatusCodes.Status422UnprocessableEntity),
            _ => Problem(added.Error, "The category can't be added", StatusCodes.Status400BadRequest)
        };
    }

    private static IResult Problem(BudgetError error, string title, int statusCode) =>
        Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
