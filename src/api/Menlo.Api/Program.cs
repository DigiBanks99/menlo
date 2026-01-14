using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;
using Menlo.Api.Auth;
using Menlo.Api.Auth.Endpoints;
using Menlo.Api.Auth.Policies;
using Menlo.Api.OpenApi;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.AddServiceDefaults();

builder
    .AddMenloAuthentication()
    .AddMenloAiWithAspire()
    .AddMenloOpenApi();

WebApplication app = builder.Build();

app.UseMenloSecurityHeaders()
    .UseAuthentication()
    .UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

string[] summaries =
[
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
];

// Map authentication endpoints (public, no auth required)
app.MapAuthEndpoints();

// Protected API endpoints
RouteGroupBuilder apiGroup = app
    .MapGroup("/api")
    .WithTags("Menlo API")
    .RequireAuthorization(MenloPolicies.RequireAuthenticated);

apiGroup
    .MapGet("/weatherforecast", () =>
    {
        WeatherForecast[] forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithSummary("Gets a 5-day weather forecast");

// Add simple AI health check endpoint
apiGroup
    .MapGet("/ai/health", async (IChatService chatService) =>
    {
        try
        {
            var response = await chatService.GetResponseAsync("Hello, respond with 'AI service is working'");
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
