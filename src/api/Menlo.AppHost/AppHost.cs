using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("postgres-menlo")
    .WithPgAdmin(); // Add PgAdmin for database management

IResourceBuilder<PostgresDatabaseResource> db = postgres
    .AddDatabase("menlo");

// Add Menlo.Api project as a distributed service
IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.Menlo_Api>("menlo-api")
    .WithReference(db)
    .WaitFor(db)
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
