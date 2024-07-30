using Menlo.Utilities;
using Microsoft.Azure.CosmosRepository.AspNetCore.Extensions;
using Microsoft.FeatureManagement;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddHealthChecks()
    .AddCosmosRepository();

builder.Services.RegisterFeatureManagement();

builder.AddUtilitiesModule();

WebApplication app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseUtilitiesModule();

app.Run();
