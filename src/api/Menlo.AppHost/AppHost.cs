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

// llama.cpp's OpenAI-compatible server in router mode: it serves the models named in llama/models.ini and loads
// one on its first request, downloading it from Hugging Face into the cache volume the first time.
// The tag is pinned; it must be new enough for the qwen35 architecture and MTP speculative decoding.
IResourceBuilder<ContainerResource> llama = builder
    .AddContainer("llama", "ggml-org/llama.cpp", "server-b12211")
    .WithImageRegistry("ghcr.io")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount(Path.Join(builder.AppHostDirectory, "llama"), "/presets", isReadOnly: true)
    .WithVolume("llama-hf-cache", "/root/.cache")
    .WithArgs("--models-preset", "/presets/models.ini", "--models-max", "1")
    .WithHttpEndpoint(name: "http", targetPort: 8080)
    .WithHttpHealthCheck("/health");

// Menlo:AI:Endpoint is the server's base URL including /v1
EndpointReference llamaEndpoint = llama.GetEndpoint("http");
ReferenceExpression aiEndpoint = ReferenceExpression.Create($"{llamaEndpoint}/v1");

// Opt-in: run the API from the production Dockerfile, which bundles the built SPA into wwwroot.
// Slower to iterate on than the Vite dev server, but exercises the real deployment (Production
// environment, production CSP, same-origin SPA). Enable with `aspire secret set Menlo:BundledUi true`
// or the `Menlo__BundledUi` environment variable.
bool bundledUi = bool.TryParse(builder.Configuration["Menlo:BundledUi"], out bool bundled) && bundled;

#pragma warning disable ASPIREBROWSERLOGS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
if (bundledUi)
{
    string repoRoot = Path.GetFullPath(Path.Join(builder.AppHostDirectory, "..", "..", ".."));

    builder
        .AddDockerfile("api-bundled", repoRoot)
        .WithHttpEndpoint(name: "http", port: 8080, targetPort: 8080)
        .WithHttpHealthCheck("/health")
        // Mirrors deployment/docker-compose.prod.yml, where Postgres does not offer SSL
        .WithEnvironment("ConnectionStrings__menlo", ReferenceExpression.Create($"{db.Resource.ConnectionStringExpression};SSL Mode=Disable"))
        .WaitFor(db)
        .WithEnvironment("Menlo__AI__Endpoint", aiEndpoint)
        .WaitFor(llama)
        .WithEntraIdCredentials(builder)
        .WithExternalHttpEndpoints();

    builder.Build().Run();
    return;
}

IResourceBuilder<ProjectResource> api = builder
    .AddProject<Projects.Menlo_Api>("api")
    .WithHttpHealthCheck("health")
    .WithReference(db)
    .WaitFor(db)
    .WithEnvironment("Menlo__AI__Endpoint", aiEndpoint)
    .WaitFor(llama)
    .WithEntraIdCredentials(builder)
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
    .WaitFor(api);

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

