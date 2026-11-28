using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;
using Menlo.Api.Antiforgery;
using Menlo.Api.Auth;
using Menlo.Api.Auth.Endpoints;
using Menlo.Api.Auth.Policies;
using Menlo.Api.Budget;
using Menlo.Api.ForwardedHeaders;
using Menlo.Api.OpenApi;
using Menlo.Api.Ui;
using Menlo.Application.Common;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.AddServiceDefaults();

builder
    .AddMenloAuthentication()
    .AddMenloForwardedHeaders()
    .AddMenloAntiforgery()
    .AddMenloUi()
    .AddMenloAi()
    .AddMenloOpenApi()
    .AddMenloBudget()
    .AddMenloApplication();

// Outside a publish the UI library's CSS, scripts and fonts are served from the build output, not wwwroot
builder.WebHost.UseStaticWebAssets();

WebApplication app = builder.Build();

app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseMenloAntiforgery()
    .UseMenloSecurityHeaders()
    .UseAuthentication()
    .UseAuthorization()
    .UseMenloUserContext();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Map authentication endpoints (public, no auth required)
app.MapAuthEndpoints();

// Protected API endpoints
RouteGroupBuilder apiGroup = app
    .MapGroup("/api")
    .WithTags("Menlo API")
    .RequireAuthorization(MenloPolicies.RequireAuthenticated);

// Budget endpoints
apiGroup.MapBudgetEndpoints();

// Add simple AI health check endpoint
apiGroup
    .MapGet("/ai/health", async (IChatService chatService) =>
    {
        try
        {
            string response = await chatService.GetResponseAsync("Hello, respond with 'AI service is working'");
            return Results.Ok(new { Status = "Healthy", Response = response });
        }
        catch (Exception ex)
        {
            return Results.Problem($"AI service unavailable: {ex.Message}");
        }
    })
    .WithName("GetAiHealth")
    .WithSummary("Gets the health status of the AI service");

app.MapDefaultEndpoints();
app.MapMenloUi();

await app.MigrateDatabaseAsync();

app.Run();
