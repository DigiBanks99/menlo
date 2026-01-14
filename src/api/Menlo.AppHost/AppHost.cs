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
    .WithExternalHttpEndpoints();

string uiPath = Path.Join(builder.AppHostDirectory, "..", "..", "ui", "web");

IResourceBuilder<JavaScriptAppResource> ui = builder
    .AddJavaScriptApp("web-ui", uiPath)
    .WithPnpm()
    .WithRunScript("start")
    .WithEnvironment("NODE_ENV", builder.Environment.IsProduction() ? "production" : "development")
    .WithHttpEndpoint(name: "https", isProxied: false, port: 4200, env: "PORT")
    .WithHttpHealthCheck()
    .WithReference(api)
    .WaitFor(api);

IResourceBuilder<JavaScriptAppResource> uiStorybook = builder
    .AddJavaScriptApp("web-ui-storybook", uiPath)
    .WithPnpm(false)
    .WithRunScript("storybook")
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "https", isProxied: false, port: 6006)
    .WithHttpHealthCheck()
    .WithExplicitStart();

IResourceBuilder<JavaScriptAppResource> libStorybook = builder
    .AddJavaScriptApp("lib-ui-storybook", uiPath)
    .WithPnpm(false)
    .WithRunScript("storybook:lib")
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "https", isProxied: false, port: 6007)
    .WithHttpHealthCheck()
    .WithExplicitStart();

uiStorybook.WithParentRelationship(ui);
libStorybook.WithParentRelationship(ui);

builder.Build().Run();
