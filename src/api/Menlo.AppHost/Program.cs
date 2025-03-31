#pragma warning disable ASPIRECOSMOSDB001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(e => e.WithDataExplorer());
var database = cosmos.AddCosmosDatabase("menlo");
var electricityUsage = database.AddContainer("ElectricityUsage", "/Id");
var electricityPurchase = database.AddContainer("ElectricityPurchase", "/Id");
var waterReading = database.AddContainer("WaterReading", "/Id");

var api = builder.AddProject<Projects.Menlo_Api>("api")
    /*.WithReference(cosmos)
    .WithEnvironment("RepositoryOptions__CosmosConnectionString", cosmos)
    .WithReference(database).WaitFor(database)
    .WithReference(electricityUsage).WaitFor(electricityUsage)
    .WithReference(electricityPurchase).WaitFor(electricityPurchase)
    .WithReference(waterReading).WaitFor(waterReading)*/;

var webUi = builder.AddNpmApp("web-ui", "../../ui/web")
    .WithHttpsEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithReference(api);

builder.Build().Run();
