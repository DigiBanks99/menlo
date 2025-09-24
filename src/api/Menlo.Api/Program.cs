using Menlo.AI.Extensions;
using Menlo.AI.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.AddServiceDefaults();

// Add Menlo AI services with Aspire integration
builder.Services.AddMenloAIWithAspire(builder);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

var securityPolicy = new HeaderPolicyCollection()
    .AddCrossOriginOpenerPolicy(policyBuilder => policyBuilder.UnsafeNone());

app.UseSecurityHeaders(securityPolicy);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/api/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Add simple AI health check endpoint
app.MapGet("/api/ai/health", async (IChatService chatService) =>
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
.WithName("GetAiHealth");

// Menlo: Expose default health endpoints in development
app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Expose Program for WebApplicationFactory in tests
public partial class Program { }
