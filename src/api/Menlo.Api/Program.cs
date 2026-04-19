using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;
using Menlo.Api.Antiforgery;
using Menlo.Api.Auth;
using Menlo.Api.Auth.Endpoints;
using Menlo.Api.Auth.Policies;
using Menlo.Api.Budget;
using Menlo.Api.OpenApi;
using Menlo.Api.SpaHosting;
using Menlo.Application.Common;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.AddServiceDefaults();

builder
    .AddMenloAuthentication()
    .AddMenloAntiforgery()
    .AddSpaReverseProxy()
    .AddMenloAiWithAspire()
    .AddMenloOpenApi()
    .AddMenloApplication();

WebApplication app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownIPNetworks = { },
    KnownProxies = { }
});

app.UseHttpsRedirection();

app.UseMenloAntiforgery()
    .UseMenloSpaStaticFiles()
    .UseMenloSecurityHeaders()
    .UseAuthentication()
    .UseAuthorization();

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
app.MapMenloSpa();

await app.MigrateDatabaseAsync();

app.Run();
