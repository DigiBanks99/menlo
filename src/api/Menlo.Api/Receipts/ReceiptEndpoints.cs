using CSharpFunctionalExtensions;
using Menlo.Api.Receipts.Photos;
using Menlo.Lib.Receipts.Errors;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Receipts;

/// <summary>
/// The JSON endpoints for a budget year's receipts. Like the rest of the budget they answer 404 while
/// <c>Features:Budget</c> is off, and every signed-in member may use them.
/// </summary>
public static class ReceiptEndpoints
{
    public static IHostApplicationBuilder AddMenloReceipts(this IHostApplicationBuilder builder)
    {
        ReceiptPhotoConverter.LimitResources();
        builder.Services.AddScoped<ReceiptHandlers>();
        return builder;
    }

    public static RouteGroupBuilder MapReceiptEndpoints(this RouteGroupBuilder api)
    {
        RouteGroupBuilder receipts = api
            .MapGroup("/budgets/{year:int}/receipts")
            .WithTags("Receipts")
            .AddEndpointFilter(OnlyWithTheBudget);

        receipts
            .MapPost("/", Send)
            .WithName("SendReceipt")
            .WithSummary("Sends 1 to 3 photos of a slip, as multipart/form-data, to be read in the background.")
            .Produces<ReceiptDto>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithMetadata(new RequestSizeLimitAttribute(ReceiptUpload.MaxRequestBytes));

        receipts
            .MapGet("/", List)
            .WithName("ListReceipts")
            .WithSummary("Lists the year's receipts not yet saved or discarded, with their status, newest first.");

        receipts
            .MapGet("/{id:guid}", Get)
            .WithName("GetReceipt")
            .WithSummary("Gets one receipt, with what was read from it.");

        receipts
            .MapGet("/{id:guid}/photos/{n:int}", GetPhoto)
            .WithName("GetReceiptPhoto")
            .WithSummary("Gets photo n of a receipt, from 1, as a JPEG.")
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg");

        receipts
            .MapDelete("/{id:guid}", Discard)
            .WithName("DiscardReceipt")
            .WithSummary("Discards a receipt, photos and all.");

        return api;
    }

    private static async ValueTask<object?> OnlyWithTheBudget(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("Features:Budget", defaultValue: false)
            ? await next(context)
            : Results.NotFound();

    private static async Task<IResult> Send(
        int year,
        HttpRequest request,
        ReceiptHandlers handlers,
        CancellationToken cancellationToken)
    {
        if (await ReceiptUpload.ReadAsync(request, cancellationToken) is not { } upload)
        {
            return Results.Problem(
                title: "Not a photo upload",
                detail: "Send the photos as multipart/form-data.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        Result<ReceiptDto, ReceiptError> sent = await handlers.SendAsync(year, upload, cancellationToken);
        return sent.IsSuccess
            ? Results.Accepted($"/api/budgets/{year}/receipts/{sent.Value.Id}", sent.Value)
            : Results.Problem(
                title: "The receipt can't be sent",
                detail: sent.Error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["code"] = sent.Error.Code });
    }

    private static async Task<IResult> List(int year, ReceiptHandlers handlers, CancellationToken cancellationToken) =>
        Results.Ok(await handlers.ListAsync(year, cancellationToken));

    private static async Task<IResult> Get(int year, Guid id, ReceiptHandlers handlers, CancellationToken cancellationToken) =>
        await handlers.GetAsync(year, id, cancellationToken) is { } receipt
            ? Results.Ok(receipt)
            : Results.NotFound();

    private static async Task<IResult> GetPhoto(
        int year,
        Guid id,
        int n,
        ReceiptHandlers handlers,
        CancellationToken cancellationToken) =>
        await handlers.GetPhotoAsync(year, id, n, cancellationToken) is { } jpeg
            ? Results.File(jpeg, "image/jpeg")
            : Results.NotFound();

    private static async Task<IResult> Discard(int year, Guid id, ReceiptHandlers handlers, CancellationToken cancellationToken) =>
        await handlers.DiscardAsync(year, id, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
}
