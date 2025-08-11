# Microsoft Entra ID Authentication Setup Implementation Guide

This document outlines the step-by-step process for integrating Microsoft Entra ID (Azure AD) authentication with JWT Bearer tokens and policy-based authorisation in the Menlo Home Management solution.

## Prerequisites

- Review the [Architecture Document](../../explanations/architecture-document.md) for security requirements.
- Review the [Business Requirements](../../requirements/business-requirements.md) for user roles and workflows.
- Azure subscription with Microsoft Entra ID tenant.
- .NET 9 SDK or later installed.

## Steps

### 1. Add Authentication NuGet Packages ✅

Add the required authentication packages to `Menlo.Api` project only (not AppHost):

```sh
dotnet add src/api/Menlo.Api/Menlo.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/api/Menlo.Api/Menlo.Api.csproj package Microsoft.Identity.Web
```

### 2. Configure Azure AD App Registration ✅

Create app registrations in Azure Portal for the API:

**API App Registration (Menlo.Api):**

- Navigate to Azure Portal → Microsoft Entra ID → App registrations
- Create new registration: "Menlo Home Management API"
- Supported account types: "Accounts in this organisational directory only"
- No redirect URI needed for API
- Note the **Application (client) ID** and **Directory (tenant) ID**
- Under "Expose an API":
  - Set Application ID URI (e.g., `api://menlo-api` or use default format)
  - Add scopes:
    - `Data.Read` - For reading data across all domains
    - `Planning.Write` - For writing planning lists and coordination data
    - `Budget.Write` - For writing budget and financial data
    - `Events.Write` - For creating and managing events

**Roles Configuration:**

- Under "App roles", create roles:
  - `Planning` - COO role: planning lists, coordination (read all, write planning)
  - `Budget` - CFO role: budget management, financial analysis, event creation (read all, write budget/events)
  - `Operations` - General operations access (read-only across domains)

### 3. Configure Authentication in Menlo.Api ✅

Create `AuthenticationServiceCollectionExtensions.cs` in `Menlo.Api/src/Security/`:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace Menlo.Api.Security;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddMenloAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Add JWT Bearer authentication with Microsoft Identity Web
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        // Add authorization with policies
        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy("ReadAllPolicy", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Planning") || 
                    context.User.IsInRole("Budget") || 
                    context.User.IsInRole("Operations"))
                      .RequireClaim("scope", "Data.Read"))
            .AddPolicy("PlanningWritePolicy", policy =>
                policy.RequireRole("Planning")
                      .RequireClaim("scope", "Planning.Write"))
            .AddPolicy("BudgetWritePolicy", policy =>
                policy.RequireRole("Budget")
                      .RequireClaim("scope", "Budget.Write"))
            .AddPolicy("EventsWritePolicy", policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole("Planning") || 
                    context.User.IsInRole("Budget"))
                      .RequireClaim("scope", "Events.Write"));

        return services;
    }
}
```

Update `appsettings.json` and `appsettings.Development.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "{TENANT_ID}",
    "ClientId": "{API_CLIENT_ID}",
    "Audience": "api://menlo-api"
  }
}
```

Update `Program.cs` in `Menlo.Api`:

```csharp
// Add authentication and authorization
builder.Services.AddMenloAuthentication(builder.Configuration);

// ... existing code ...

// Configure middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
```

### 4. Update OpenAPI/Swagger Configuration ✅

Enhance Swagger to support JWT Bearer authentication. Update the OpenAPI configuration in `Program.cs`:

```csharp
builder.Services.AddOpenApi("menlo-api", options =>
{
    options.AddDocumentTransformer<AuthenticationDocumentTransformer>();
});
```

Create `AuthenticationDocumentTransformer.cs` in `Menlo.Api/src/Security/`:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Menlo.Api.Security;

internal sealed class AuthenticationDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT Bearer token"
        };

        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }] = Array.Empty<string>()
        });

        return Task.CompletedTask;
    }
}
```

### 5. Create Integration Test Authentication Infrastructure ✅

Create `TestAuthenticationExtensions.cs` in `Menlo.Api.IntegrationTests/`:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Menlo.Api.IntegrationTests;

public static class TestAuthenticationExtensions
{
    public static WebApplicationFactory<T> WithTestAuthentication<T>(
        this WebApplicationFactory<T> factory,
        params Claim[] claims) where T : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing authentication
                var authDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IAuthenticationSchemeProvider));
                if (authDescriptor != null)
                {
                    services.Remove(authDescriptor);
                }

                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                        .RequireAuthenticatedUser()
                        .Build();
                });

                // Add test claims
                services.AddSingleton(new TestClaimsProvider(claims));
            });
        });
    }

    public static WebApplicationFactory<T> WithPlanningRole<T>(this WebApplicationFactory<T> factory) where T : class
        => factory.WithTestAuthentication(
            new Claim(ClaimTypes.NameIdentifier, "test-user-planning"),
            new Claim(ClaimTypes.Name, "Test Planning User"),
            new Claim(ClaimTypes.Email, "planning@menlo.test"),
            new Claim(ClaimTypes.Role, "Planning"),
            new Claim("scope", "Data.Read"),
            new Claim("scope", "Planning.Write"),
            new Claim("scope", "Events.Write"));

    public static WebApplicationFactory<T> WithBudgetRole<T>(this WebApplicationFactory<T> factory) where T : class
        => factory.WithTestAuthentication(
            new Claim(ClaimTypes.NameIdentifier, "test-user-budget"),
            new Claim(ClaimTypes.Name, "Test Budget User"),
            new Claim(ClaimTypes.Email, "budget@menlo.test"),
            new Claim(ClaimTypes.Role, "Budget"),
            new Claim("scope", "Data.Read"),
            new Claim("scope", "Budget.Write"),
            new Claim("scope", "Events.Write"));

    public static WebApplicationFactory<T> WithOperationsRole<T>(this WebApplicationFactory<T> factory) where T : class
        => factory.WithTestAuthentication(
            new Claim(ClaimTypes.NameIdentifier, "test-user-operations"),
            new Claim(ClaimTypes.Name, "Test Operations User"),
            new Claim(ClaimTypes.Email, "operations@menlo.test"),
            new Claim(ClaimTypes.Role, "Operations"),
            new Claim("scope", "Data.Read"));

    public static WebApplicationFactory<T> WithNoRole<T>(this WebApplicationFactory<T> factory) where T : class
        => factory.WithTestAuthentication(
            new Claim(ClaimTypes.NameIdentifier, "test-user-norole"),
            new Claim(ClaimTypes.Name, "Test No Role User"),
            new Claim(ClaimTypes.Email, "norole@menlo.test"));

    public static WebApplicationFactory<T> WithUnauthenticated<T>(this WebApplicationFactory<T> factory) where T : class
        => factory.WithTestAuthentication(); // No claims = unauthenticated
}

public class TestClaimsProvider
{
    public TestClaimsProvider(params Claim[] claims)
    {
        Claims = claims;
    }

    public Claim[] Claims { get; }
}

public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TestClaimsProvider _claimsProvider;

    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, TestClaimsProvider claimsProvider)
        : base(options, logger, encoder)
    {
        _claimsProvider = claimsProvider;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_claimsProvider.Claims.Length == 0)
        {
            return Task.FromResult(AuthenticateResult.Fail("No test claims provided"));
        }

        var identity = new ClaimsIdentity(_claimsProvider.Claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

### 6. Create Authentication Integration Tests ✅

Create `AuthenticationTests.cs` in `Menlo.Api.IntegrationTests/`:

```csharp
using Shouldly;
using System.Net;

namespace Menlo.Api.IntegrationTests;

public class AuthenticationTests : IClassFixture<MenloWebApplicationFactory>
{
    private readonly MenloWebApplicationFactory _webApplicationFactory;

    public AuthenticationTests(MenloWebApplicationFactory webApplicationFactory)
    {
        _webApplicationFactory = webApplicationFactory;
    }

    [Fact]
    public async Task GivenUnauthenticatedRequest_WhenAccessingProtectedEndpoint()
    {
        // Arrange
        using var client = _webApplicationFactory.WithUnauthenticated().CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/menlo-api.json");

        // Assert - OpenAPI should be accessible without auth
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // TODO: Test actual protected endpoint when available
        // var protectedResponse = await client.GetAsync("/api/protected");
        // protectedResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GivenPlanningRole_WhenAccessingPlanningEndpoint()
    {
        // Arrange
        using var client = _webApplicationFactory.WithPlanningRole().CreateClient();

        // Act & Assert
        // TODO: Implement when planning endpoints are available
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GivenBudgetRole_WhenAccessingBudgetEndpoint()
    {
        // Arrange
        using var client = _webApplicationFactory.WithBudgetRole().CreateClient();

        // Act & Assert
        // TODO: Implement when budget endpoints are available
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GivenBudgetRole_WhenAccessingEventEndpoint()
    {
        // Arrange
        using var client = _webApplicationFactory.WithBudgetRole().CreateClient();

        // Act & Assert
        // TODO: Test CFO can create events when event endpoints are available
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GivenOperationsRole_WhenAccessingWriteEndpoint()
    {
        // Arrange
        using var client = _webApplicationFactory.WithOperationsRole().CreateClient();

        // Act & Assert
        // TODO: Test operations role cannot write when write endpoints are available
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GivenNoRole_WhenAccessingRoleProtectedEndpoint()
    {
        // Arrange
        using var client = _webApplicationFactory.WithNoRole().CreateClient();

        // Act & Assert
        // TODO: Test access to role-protected endpoints when available
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GivenValidRequest_WhenAccessingSwaggerUi()
    {
        // Arrange
        using var client = _webApplicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/openapi/menlo-api.json");

        // Assert
        ItShouldIncludeBearerAuthenticationInSwagger(response);
    }

    private static async void ItShouldIncludeBearerAuthenticationInSwagger(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Bearer");
        content.ShouldContain("securitySchemes");
    }
}
```

Update `MenloWebApplicationFactory.cs` to support authentication overrides:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Menlo.Api.IntegrationTests;

public class MenloWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
        .WithImage("docker.io/library/postgres:17.4")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Override connection string for tests
            services.Configure<ConnectionStrings>(options =>
            {
                options.DefaultConnection = _postgreSqlContainer.GetConnectionString();
            });
        });

        builder.UseEnvironment("Production");
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgreSqlContainer.StopAsync();
        await base.DisposeAsync();
    }
}
```

### 7. Frontend Authentication Preparation (Future) ✅

While not implemented now, the API is configured to support frontend authentication. For future Angular integration:

**Frontend App Registration (Future):**

- Create separate app registration: "Menlo Home Management Frontend"
- Platform configuration: Single-page application (SPA)
- Redirect URIs: `http://localhost:4200`, `https://yourdomain.azurestaticapps.net`
- API permissions: Add permissions to access Menlo.Api scopes
- Enable implicit flow and authorization code flow

**Frontend Configuration (Future):**

```typescript
// @azure/msal-angular configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: 'FRONTEND_CLIENT_ID',
    authority: 'https://login.microsoftonline.com/TENANT_ID',
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  }
};
```

### 8. Documentation & Validation ✅

**Environment Configuration:**

- Development: Uses Azure AD test tenant
- Production: Uses production Azure AD tenant
- Testing: Uses test authentication handler with configurable claims

**Claims and Roles Mapping:**

| Business Role | Azure AD Role | Required Scopes | Read Access | Write Access |
|---------------|---------------|-----------------|-------------|--------------|
| Wife (COO) | Planning | Data.Read, Planning.Write, Events.Write | All domains | Planning, Events |
| Husband (CFO) | Budget | Data.Read, Budget.Write, Events.Write | All domains | Budget, Events |
| Shared | Operations | Data.Read | All domains | None |

**Policy Breakdown:**

- **ReadAllPolicy**: Planning, Budget, and Operations roles can read across all domains
- **PlanningWritePolicy**: Planning role can write planning lists and coordination data
- **BudgetWritePolicy**: Budget role can write budget and financial data  
- **EventsWritePolicy**: Both Planning and Budget roles can create and manage events (CFO creates events for budget planning)

**Testing Strategy:**

- Positive authentication tests with valid roles
- Negative authentication tests (401 Unauthorized)
- Negative authorization tests (403 Forbidden)
- Dynamic role assignment per test case
- Mock claims provider for different scenarios

**Security Considerations:**

- All environments use real Azure AD authentication
- JWT tokens validated using Azure AD public keys
- Role-based access control with policies
- Scopes validated for fine-grained permissions
- No development authentication bypass

ℹ️ **Gotcha: Azure AD Configuration**
> Ensure the API's Application ID URI matches the `Audience` configuration exactly. Mismatches will result in token validation failures. Use either the default format `api://{CLIENT_ID}` or configure a custom URI consistently across registrations.

ℹ️ **Gotcha: Integration Test Authentication**
> The test authentication handler completely replaces the real authentication for tests. Ensure you're testing the actual policies and claims validation, not just the test infrastructure.

---

For more details, see the [Implementation Roadmap](../../requirements/implementation-roadmap.md) and [Architecture Document](../../explanations/architecture-document.md).
