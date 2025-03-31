using Menlo.Auth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Azure.CosmosRepository.AspNetCore.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddHealthChecks()
    .AddCosmosRepository();

builder.Services
    .Configure<ForwardedHeadersOptions>(options =>
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

AuthorizationBuilder authBuilder = builder.AddAuth();

builder.Services.AddControllersWithViews();

builder.Services
    .AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.RegisterFeatureManagement();

builder.AddUtilitiesModule(authBuilder);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

WebApplication app = builder.Build();

app.UseForwardedHeaders();

app.UseSecurityHeaders(new HeaderPolicyCollection()
    .AddFrameOptionsDeny()
    .AddContentTypeOptionsNoSniff()
    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
    .AddCrossOriginOpenerPolicy(policyBuilder => policyBuilder.SameOrigin())
    .AddCrossOriginResourcePolicy(policyBuilder => policyBuilder.SameOrigin())
    .AddCrossOriginEmbedderPolicy(policyBuilder => policyBuilder.RequireCorp())
    .RemoveServerHeader()
    .ApplyDocumentHeadersToAllResponses());

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    app.UseFileServer();
}

app.UseUtilitiesModule();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapReverseProxy();
}

app.MapFallbackToFile("index.html")
    .RequireAuthorization(AuthConstants.PolicyNameAuthenticatedUsersOnly)
    .CacheOutput(p => p.NoCache());

app.Run();
