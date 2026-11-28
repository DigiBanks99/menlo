using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Budget;

/// <summary>
/// The JSON endpoints for the household's rental units. A rental unit belongs to the household, not to a budget, so
/// they aren't under a year. They answer 404 while <c>Features:Budget</c> is off, and every signed-in member may use
/// them.
/// </summary>
public static class RentalUnitEndpoints
{
    public static IHostApplicationBuilder AddMenloRentalUnits(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<RentalUnitHandlers>();
        return builder;
    }

    public static RouteGroupBuilder MapRentalUnitEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder units = api
            .MapGroup("/rental-units")
            .WithTags("Rental units")
            .AddEndpointFilter(BudgetEndpoints.OnlyWithTheBudget);

        units
            .MapPost("/", Create)
            .WithName("CreateRentalUnit")
            .WithSummary("Adds a rental unit to the household.")
            .Produces<RentalUnitDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        units
            .MapGet("/", List)
            .WithName("ListRentalUnits")
            .WithSummary("Lists the household's rental units, by name.")
            .Produces<IReadOnlyList<RentalUnitDto>>();

        units
            .MapGet("/{id:guid}", Get)
            .WithName("GetRentalUnit")
            .WithSummary("Gets a rental unit.")
            .Produces<RentalUnitDto>()
            .Produces(StatusCodes.Status404NotFound);

        units
            .MapPatch("/{id:guid}", Update)
            .WithName("UpdateRentalUnit")
            .WithSummary("Renames a rental unit. Its identity never changes.")
            .Produces<RentalUnitDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<IResult> Create(
        CreateRentalUnitRequest request,
        RentalUnitHandlers handlers,
        CancellationToken cancellationToken)
    {
        Result<RentalUnitDto, BudgetError> created = await handlers.CreateAsync(request.Name, cancellationToken);
        return created.IsSuccess
            ? Results.Created($"/api/rental-units/{created.Value.Id}", created.Value)
            : Problem(created.Error, "The rental unit can't be added");
    }

    private static async Task<IResult> List(RentalUnitHandlers handlers, CancellationToken cancellationToken) =>
        Results.Ok(await handlers.ListAsync(cancellationToken));

    private static async Task<IResult> Get(Guid id, RentalUnitHandlers handlers, CancellationToken cancellationToken) =>
        await handlers.GetAsync(id, cancellationToken) is { } unit
            ? Results.Ok(unit)
            : Results.NotFound();

    private static async Task<IResult> Update(
        Guid id,
        UpdateRentalUnitRequest request,
        RentalUnitHandlers handlers,
        CancellationToken cancellationToken)
    {
        Result<RentalUnitDto?, BudgetError> updated = await handlers.UpdateAsync(id, request.Name, cancellationToken);
        if (updated.IsFailure)
        {
            return Problem(updated.Error, "The rental unit can't be changed");
        }

        return updated.Value is { } unit ? Results.Ok(unit) : Results.NotFound();
    }

    private static IResult Problem(BudgetError error, string title) =>
        Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
