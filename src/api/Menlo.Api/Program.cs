using Menlo.Auth;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Azure.CosmosRepository.AspNetCore.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddHealthChecks()
    .AddCosmosRepository();

builder.Services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration);

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(AuthConstants.PolicyNameAuthenticatedUsersOnly, op => op.RequireAuthenticatedUser().Build())
    .AddUtilitiesPolicy();

builder.Services.AddControllersWithViews();

builder.Services
    .AddRazorPages()
    .AddMicrosoftIdentityUI();

builder.Services.RegisterFeatureManagement();

builder.AddUtilitiesModule();

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

WebApplication app = builder.Build();

app.UseForwardedHeaders();

app.UseSecurityHeaders(new HeaderPolicyCollection()
    .AddFrameOptionsDeny()
    .AddContentTypeOptionsNoSniff()
    .AddReferrerPolicyStrictOriginWhenCrossOrigin()
    .AddCrossOriginOpenerPolicy(builder => builder.SameOrigin())
    .AddCrossOriginResourcePolicy(builder => builder.SameOrigin())
    .AddCrossOriginEmbedderPolicy(builder => builder.RequireCorp())
    .RemoveServerHeader()
    .ApplyDocumentHeadersToAllResponses());

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
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
    .RequireAuthorization(AuthConstants.PolicyNameAuthenticatedUsersOnly);

app.Run();
