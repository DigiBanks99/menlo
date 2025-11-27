using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL
IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("postgres-menlo")
    .WithPgAdmin(); // Add PgAdmin for database management

IResourceBuilder<PostgresDatabaseResource> db = postgres
    .AddDatabase("menlo");

// Add Ollama with automatic model bootstrapping and persistent storage
var ollama = builder.AddOllama("ollama")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("ollama-models") // Persist models across container restarts
    .WithOpenWebUI(); // Optional: Add Open WebUI for model testing

// Add models for different AI capabilities
var phi4Mini = ollama.AddModel("text", "phi4-mini:latest"); // Text processing
var phi4Vision = ollama.AddModel("vision", "qwen2.5vl:3b"); // Vision processing

// Alternative lightweight models for development
// var phi35 = ollama.AddModel("phi35-mini", "phi3.5:3.8b"); // Smaller text model
// var llava = ollama.AddModel("llava-vision", "llava:latest"); // Alternative vision model

// Add Menlo.Api project with AI dependencies
IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.Menlo_Api>("api")
    .WithReference(db)
    .WithReference(phi4Mini)      // Reference text model
    .WithReference(phi4Vision)    // Reference vision model
    .WaitFor(db)
    .WaitFor(phi4Mini)           // Wait for models to be ready
    .WaitFor(phi4Vision)
    .WithOtlpExporter(); // Enable telemetry

string uiPath = Path.Join(builder.AppHostDirectory, "..", "..", "ui", "web");
IResourceBuilder<NodeAppResource> ui = builder
    .AddPnpmApp("web-ui", uiPath)
    .WithPnpmPackageInstallation()
    .WithEnvironment("NODE_ENV", builder.Environment.IsProduction() ? "production" : "development")
    .WithHttpEndpoint(name: "https", isProxied: false, port: 4200, env: "PORT")
    .WithHttpHealthCheck()
    .WithReference(api)
    .WaitFor(api);

IResourceBuilder<NodeAppResource> uiStorybook = builder
    .AddPnpmApp("web-ui-storybook", uiPath, "storybook")
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "https", isProxied: false, port: 6006)
    .WithHttpHealthCheck()
    .WithExplicitStart();

IResourceBuilder<NodeAppResource> libStorybook = builder
    .AddPnpmApp("lib-ui-storybook", uiPath, "storybook:lib")
    .WithExternalHttpEndpoints()
    .WithHttpEndpoint(name: "https", isProxied: false, port: 6007)
    .WithHttpHealthCheck()
    .WithExplicitStart();

uiStorybook.WithParentRelationship(ui);
libStorybook.WithParentRelationship(ui);

builder.Build().Run();
