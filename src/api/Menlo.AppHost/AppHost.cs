using Aspire.Hosting.JavaScript;
using Menlo.AppHost;
using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("postgres-menlo")
    .WithPgAdmin();

IResourceBuilder<PostgresDatabaseResource> db = postgres
    .AddDatabase("menlo");

IResourceBuilder<OllamaResource> ollama = builder.AddOllama("ollama")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ollama-models")
    .WithOpenWebUI();

IResourceBuilder<OllamaModelResource> textModel = ollama.AddModel("text", "phi4-mini:latest"); // Text processing
IResourceBuilder<OllamaModelResource> visionModel = ollama.AddModel("vision", "qwen2.5vl:3b"); // Vision processing

#pragma warning disable ASPIREBROWSERLOGS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.Menlo_Api>("api")
    .WithHttpHealthCheck("health")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(textModel)
    .WaitFor(textModel)
    .WithReference(visionModel)
    .WaitFor(visionModel)
    .WithEnvironment(env => env.AddEntraIdCredentials(builder.Configuration))
    .WithExternalHttpEndpoints()
    .WithBrowserLogs();

string uiPath = Path.Join(builder.AppHostDirectory, "..", "..", "ui", "web");

IResourceBuilder<JavaScriptAppResource> ui = builder
    .AddViteApp("web-ui", uiPath)
    .WithPnpm()
    .WithRunScript("start")
    .WithEnvironment("NODE_ENV", builder.Environment.IsProduction() ? "production" : "development")
    .WithEnvironment("HOST", "127.0.0.1")
    .WithHttpEndpoint(name: "http", isProxied: false, port: 4200, env: "PORT")
    .WithHttpHealthCheck()
    .WithReference(api)
    .WaitFor(api)
    .WithBrowserLogs();

IResourceBuilder<JavaScriptAppResource> uiStorybook = builder
    .AddJavaScriptApp("web-ui-storybook", uiPath)
    .WithPnpm(false)
    .WithRunScript("storybook")
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "https", isProxied: false, port: 6006)
    .WithHttpHealthCheck()
    .WithExplicitStart()
    .WithBrowserLogs();

IResourceBuilder<JavaScriptAppResource> libStorybook = builder
    .AddJavaScriptApp("lib-ui-storybook", uiPath)
    .WithPnpm(false)
    .WithRunScript("storybook:lib")
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "https", isProxied: false, port: 6007)
    .WithHttpHealthCheck()
    .WithExplicitStart()
    .WithBrowserLogs();
#pragma warning restore ASPIREBROWSERLOGS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

uiStorybook.WithParentRelationship(ui);
libStorybook.WithParentRelationship(ui);
api.WithReference(ui);

builder.Build().Run();

