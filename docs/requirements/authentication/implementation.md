# Authentication - Implementation Plan

- [1. Overview](#1-overview)
- [2. Folder Structure](#2-folder-structure)
  - [2.1. Backend Structure (`Menlo.Api`)](#21-backend-structure-menloapi)
  - [2.2. Library Structure (`Menlo.Lib`)](#22-library-structure-menlolib)
  - [2.3. Frontend Structure (`menlo-app`)](#23-frontend-structure-menlo-app)
  - [2.4. Test Structure](#24-test-structure)
- [3. Backend Implementation](#3-backend-implementation)
  - [3.1. Dependencies](#31-dependencies)
  - [3.2. Security Headers](#32-security-headers)
  - [3.3. Configuration Options](#33-configuration-options)
  - [3.4. Policy Definitions](#34-policy-definitions)
  - [3.5. Minimal API Endpoints](#35-minimal-api-endpoints)
  - [3.6. Authentication Schemes](#36-authentication-schemes)
  - [3.7. Service Registration](#37-service-registration)
- [4. Domain Layer (Menlo.Lib)](#4-domain-layer-menlolib)
  - [4.1. User Entity](#41-user-entity)
  - [4.2. Auth Errors](#42-auth-errors)
  - [4.3. Value Objects](#43-value-objects)
- [5. Frontend Implementation (Angular)](#5-frontend-implementation-angular)
  - [5.1. Auth Service](#51-auth-service)
  - [5.2. HTTP Interceptor](#52-http-interceptor)
  - [5.3. Auth Guards](#53-auth-guards)
  - [5.4. User Context Component](#54-user-context-component)
- [6. Testing Strategy](#6-testing-strategy)
  - [6.1. Backend Test Authentication Handler](#61-backend-test-authentication-handler)
  - [6.2. Backend Integration Tests](#62-backend-integration-tests)
  - [6.3. Angular Unit Tests](#63-angular-unit-tests)
- [7. Security Considerations](#7-security-considerations)
- [8. Implementation Checklist](#8-implementation-checklist)
  - [Backend Tasks](#backend-tasks)
  - [Library Tasks](#library-tasks)
  - [Frontend Tasks](#frontend-tasks)
  - [Testing Tasks](#testing-tasks)
  - [Configuration Tasks](#configuration-tasks)

---

## 1. Overview

This document provides a detailed implementation plan for the Authentication & Identity module. The implementation follows the **Vertical Slice Architecture** pattern, treating Authentication as its own slice with clearly separated API concerns (`Menlo.Api`) and domain/application concerns (`Menlo.Lib`).

**Key Architectural Decisions:**

- **BFF Pattern:** Backend handles OIDC; browser uses `HttpOnly` cookies.
- **Minimal APIs:** All endpoints use `MapGroup`/`MapGet`/`MapPost`.
- **Policy-Based Authorization:** Policies defined centrally; endpoints require policies.
- **Zero Information Leakage:** Frontend does not know tenant/client IDs.
- **C# 14 Features:** Use `field` keyword for auto-property backing fields.
- **Immutable Domain Models:** No setters; use factory methods and domain methods.
- **EF Core Hydration:** Private constructors with init-only properties for persistence.

---

## 2. Folder Structure

### 2.1. Backend Structure (`Menlo.Api`)

```text
src/api/Menlo.Api/
├── Auth/
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs           # Static class to map all auth endpoints
│   │   ├── LoginEndpoint.cs           # GET /auth/login
│   │   ├── LogoutEndpoint.cs          # POST /auth/logout
│   │   └── UserEndpoint.cs            # GET /auth/user
│   ├── Options/
│   │   ├── MenloAuthOptions.cs        # Auth configuration POCO
│   │   └── MenloAuthOptionsValidator.cs
│   ├── Policies/
│   │   ├── MenloPolicies.cs           # Static class with policy name constants
│   │   └── AuthPoliciesExtensions.cs  # Extension to register policies
│   └── AuthServiceCollectionExtensions.cs  # Wires up all auth services
├── Program.cs                         # Minimal startup; calls extensions
└── ...
```

### 2.2. Library Structure (`Menlo.Lib`)

```text
src/lib/Menlo.Lib/
├── Auth/
│   ├── Entities/
│   │   └── User.cs                    # Domain User entity
│   ├── Errors/
│   │   └── AuthErrors.cs              # Auth-specific errors
│   ├── ValueObjects/
│   │   ├── UserId.cs                  # Strongly typed ID
│   │   └── ExternalUserId.cs          # Entra OID wrapper
│   └── Models/
│       └── UserProfile.cs             # DTO for /auth/user response
├── Common/
│   └── ...                            # Existing abstractions
└── ...
```

### 2.3. Frontend Structure (`menlo-app`)

```text
src/ui/web/projects/menlo-app/src/app/
├── core/
│   └── auth/
│       ├── auth.service.ts            # Handles login/logout/user state
│       ├── auth.guard.ts              # CanActivate guard for authenticated routes
│       ├── role.guard.ts              # CanActivate guard for role-based routes
│       ├── auth.interceptor.ts        # Adds withCredentials to requests
│       ├── auth.models.ts             # UserProfile interface
│       └── index.ts                   # Barrel export
└── ...
```

### 2.4. Test Structure

```text
src/api/Menlo.Api.Tests/
├── Auth/
│   ├── AuthEndpointTests.cs           # Integration tests for auth endpoints
│   └── PolicyAuthorizationTests.cs    # Tests for policy enforcement
├── Fixtures/
│   ├── TestAuthHandler.cs             # Mock AuthenticationHandler
│   └── AuthenticatedWebApplicationFactory.cs
└── ...

src/ui/web/projects/menlo-app/
├── src/app/core/auth/
│   ├── auth.service.spec.ts
│   ├── auth.guard.spec.ts
│   └── ...
└── ...
```

---

## 3. Backend Implementation

### 3.1. Dependencies

Add the following NuGet packages to `Menlo.Api.csproj`:

```xml
<PackageReference Include="Microsoft.Identity.Web" Version="3.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.*" />
<PackageReference Include="NetEscapades.AspNetCore.SecurityHeaders" Version="1.3.1" />
```

### 3.2. Security Headers

Use `NetEscapades.AspNetCore.SecurityHeaders` for comprehensive security header configuration.

**File:** `Auth/SecurityHeadersExtensions.cs`

```csharp
namespace Menlo.Api.Auth;

using NetEscapades.AspNetCore.SecurityHeaders;

public static class SecurityHeadersExtensions
{
    public static HeaderPolicyCollection AddMenloSecurityHeaders(this HeaderPolicyCollection policies)
    {
        policies.AddDefaultApiSecurityHeaders();
        
        // Strengthen HSTS for production
        policies.AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: 63072000);
        
        // Configure CSP for API
        policies.AddContentSecurityPolicy(builder =>
        {
            builder.AddDefaultSrc().None();
            builder.AddFrameAncestors().None();
            builder.AddFormAction().Self();
        });
        
        return policies;
    }
}
```

### 3.3. Configuration Options

**File:** `Auth/Options/MenloAuthOptions.cs`

```csharp
namespace Menlo.Api.Auth.Options;

public sealed class MenloAuthOptions
{
    public const string SectionName = "AzureAd";

    public required string Instance { get; init; }
    public required string TenantId { get; init; }
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public required string CookieDomain { get; init; }
    public string CallbackPath { get; init; } = "/signin-oidc";
    public string SignedOutCallbackPath { get; init; } = "/signout-oidc";
}
```

**File:** `Auth/Options/MenloAuthOptionsValidator.cs`

```csharp
namespace Menlo.Api.Auth.Options;

using Microsoft.Extensions.Options;

public sealed class MenloAuthOptionsValidator : IValidateOptions<MenloAuthOptions>
{
    public ValidateOptionsResult Validate(string? name, MenloAuthOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Instance))
            return ValidateOptionsResult.Fail("Instance is required.");
        if (string.IsNullOrWhiteSpace(options.TenantId))
            return ValidateOptionsResult.Fail("TenantId is required.");
        if (string.IsNullOrWhiteSpace(options.ClientId))
            return ValidateOptionsResult.Fail("ClientId is required.");
        if (string.IsNullOrWhiteSpace(options.CookieDomain))
            return ValidateOptionsResult.Fail("CookieDomain is required.");

        return ValidateOptionsResult.Success;
    }
}
```

### 3.4. Policy Definitions

**File:** `Auth/Policies/MenloPolicies.cs`

```csharp
namespace Menlo.Api.Auth.Policies;

public static class MenloPolicies
{
    // Policy names
    public const string RequireAuthenticated = nameof(RequireAuthenticated);
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string CanEditBudget = nameof(CanEditBudget);
    public const string CanViewBudget = nameof(CanViewBudget);

    // Role values (must match Entra App Roles)
    public static class Roles
    {
        public const string Admin = "Menlo.Admin";
        public const string User = "Menlo.User";
        public const string Reader = "Menlo.Reader";
    }
}
```

**File:** `Auth/Policies/AuthPoliciesExtensions.cs`

```csharp
namespace Menlo.Api.Auth.Policies;

using Microsoft.AspNetCore.Authorization;

public static class AuthPoliciesExtensions
{
    public static AuthorizationBuilder AddMenloPolicies(this AuthorizationBuilder builder)
    {
        builder.AddPolicy(MenloPolicies.RequireAuthenticated, policy =>
            policy.RequireAuthenticatedUser());

        builder.AddPolicy(MenloPolicies.RequireAdmin, policy =>
            policy.RequireRole(MenloPolicies.Roles.Admin));

        builder.AddPolicy(MenloPolicies.CanEditBudget, policy =>
            policy.RequireRole(MenloPolicies.Roles.Admin, MenloPolicies.Roles.User));

        builder.AddPolicy(MenloPolicies.CanViewBudget, policy =>
            policy.RequireRole(
                MenloPolicies.Roles.Admin,
                MenloPolicies.Roles.User,
                MenloPolicies.Roles.Reader));

        return builder;
    }
}
```

### 3.5. Minimal API Endpoints

**File:** `Auth/Endpoints/AuthEndpoints.cs`

```csharp
namespace Menlo.Api.Auth.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/auth")
            .WithTags("Authentication");

        LoginEndpoint.Map(group);
        LogoutEndpoint.Map(group);
        UserEndpoint.Map(group);

        return app;
    }
}
```

**File:** `Auth/Endpoints/LoginEndpoint.cs`

```csharp
namespace Menlo.Api.Auth.Endpoints;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

public static class LoginEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/login", HandleAsync)
            .WithName("Login")
            .WithSummary("Initiates OIDC login flow")
            .AllowAnonymous();
    }

    private static IResult HandleAsync(HttpContext context, string? returnUrl = "/")
    {
        // Prevent open redirect attacks
        if (string.IsNullOrEmpty(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            returnUrl = "/";
        }

        AuthenticationProperties properties = new()
        {
            RedirectUri = returnUrl
        };

        return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
    }
}
```

**File:** `Auth/Endpoints/LogoutEndpoint.cs`

```csharp
namespace Menlo.Api.Auth.Endpoints;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

public static class LogoutEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/logout", HandleAsync)
            .WithName("Logout")
            .WithSummary("Signs out the user")
            .RequireAuthorization();
    }

    private static IResult HandleAsync(HttpContext context, string? returnUrl = "/")
    {
        AuthenticationProperties properties = new()
        {
            RedirectUri = returnUrl
        };

        return Results.SignOut(
            properties,
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
    }
}
```

**File:** `Auth/Endpoints/UserEndpoint.cs`

```csharp
namespace Menlo.Api.Auth.Endpoints;

using Menlo.Api.Auth.Policies;
using Menlo.Lib.Auth.Models;
using System.Security.Claims;

public static class UserEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/user", Handle)
            .WithName("GetCurrentUser")
            .WithSummary("Returns the current user's profile and roles")
            .RequireAuthorization(MenloPolicies.RequireAuthenticated)
            .Produces<UserProfile>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static IResult Handle(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        UserProfile profile = new(
            Id: user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Email: user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            DisplayName: user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Roles: user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        );

        return Results.Ok(profile);
    }
}
```

### 3.6. Authentication Schemes

**File:** `Auth/AuthServiceCollectionExtensions.cs`

```csharp
namespace Menlo.Api.Auth;

using Menlo.Api.Auth.Options;
using Menlo.Api.Auth.Policies;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddMenloAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate options
        services.AddOptions<MenloAuthOptions>()
            .BindConfiguration(MenloAuthOptions.SectionName)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<MenloAuthOptions>, MenloAuthOptionsValidator>();

        MenloAuthOptions authOptions = configuration
            .GetSection(MenloAuthOptions.SectionName)
            .Get<MenloAuthOptions>()!;

        // Configure authentication with dual schemes
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = ".Menlo.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Domain = authOptions.CookieDomain;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    // Return 401 for API calls instead of redirect
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = $"{authOptions.Instance}{authOptions.TenantId}/v2.0";
                options.ClientId = authOptions.ClientId;
                options.ClientSecret = authOptions.ClientSecret;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.CallbackPath = authOptions.CallbackPath;
                options.SignedOutCallbackPath = authOptions.SignedOutCallbackPath;
                options.TokenValidationParameters.RoleClaimType = "roles";
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = $"{authOptions.Instance}{authOptions.TenantId}/v2.0";
                options.Audience = authOptions.ClientId;
                options.TokenValidationParameters.RoleClaimType = "roles";
            });

        // Configure authorization policies
        services.AddAuthorizationBuilder()
            .AddMenloPolicies();

        return services;
    }
}
```

### 3.7. Service Registration

**File:** `Program.cs` (additions)

```csharp
// Add after builder.AddServiceDefaults();
builder.Services.AddMenloAuthentication(builder.Configuration);

// Add after var app = builder.Build();

// Security headers middleware - should be early in the pipeline
app.UseSecurityHeaders(policies => policies.AddMenloSecurityHeaders());

app.UseAuthentication();
app.UseAuthorization();

// Add endpoint mapping
app.MapAuthEndpoints();

// Apply default authorization to API endpoints
app.MapGroup("/api")
    .RequireAuthorization(MenloPolicies.RequireAuthenticated);
```

---

## 4. Domain Layer (Menlo.Lib)

### 4.1. User Entity

**File:** `Auth/Entities/User.cs`

Uses C# 14 `field` keyword to eliminate explicit backing fields. No default constructor - EF Core can hydrate via the private constructor with all required parameters.

```csharp
namespace Menlo.Lib.Auth.Entities;

using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.Abstractions;

/// <summary>
/// Aggregate root representing a system user linked to an external identity provider.
/// </summary>
public sealed class User : IAggregateRoot<UserId>, IAuditable
{
    /// <summary>
    /// Private constructor for EF Core hydration.
    /// EF Core can use this constructor to set all properties via constructor binding.
    /// </summary>
    private User(
        UserId id,
        ExternalUserId externalId,
        string email,
        string displayName,
        DateTimeOffset? lastLoginAt,
        UserId? createdBy,
        DateTimeOffset? createdAt,
        UserId? modifiedBy,
        DateTimeOffset? modifiedAt)
    {
        Id = id;
        ExternalId = externalId;
        Email = email;
        DisplayName = displayName;
        LastLoginAt = lastLoginAt;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedBy = modifiedBy;
        ModifiedAt = modifiedAt;
    }

    public UserId Id { get; }
    public ExternalUserId ExternalId { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public DateTimeOffset? LastLoginAt { get; private init; }

    // IAuditable
    public UserId? CreatedBy { get; private init; }
    public DateTimeOffset? CreatedAt { get; private init; }
    public UserId? ModifiedBy { get; private init; }
    public DateTimeOffset? ModifiedAt { get; private init; }

    // IHasDomainEvents
    public IReadOnlyCollection<IDomainEvent> DomainEvents => field ??= new List<IDomainEvent>;

    public void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        ((List<IDomainEvent>)DomainEvents).Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        ((List<IDomainEvent>)DomainEvents).Clear();
    }

    public void Audit(IAuditStampFactory factory, AuditOperation operation)
    {
        AuditStamp stamp = factory.CreateStamp();
        if (operation == AuditOperation.Create)
        {
            CreatedBy = stamp.ActorId;
            CreatedAt = stamp.Timestamp;
        }
        ModifiedBy = stamp.ActorId;
        ModifiedAt = stamp.Timestamp;
    }

    /// <summary>
    /// Factory method to create a new User.
    /// </summary>
    public static Result<User, Error> Create(ExternalUserId externalId, string email, string displayName)
    {
        return email
            .Validate() // Will require creating a string extension that returns a Result<string, ValidationError>
            .Bind(_ => displayName.Validate())
            .Map(_ => new User(
                id: UserId.Create(),
                externalId: externalId,
                email: email,
                displayName: displayName,
                lastLoginAt: DateTimeOffset.UtcNow,
                createdBy: null,
                createdAt: null,
                modifiedBy: null,
                modifiedAt: null));
    }

    /// <summary>
    /// Records a login event for this user.
    /// </summary>
    public void RecordLogin(DateTimeOffset time)
    {
        LastLoginAt = time;
        AddDomainEvent(new UserLoggedInEvent(Id));
    }
}

/// <summary>
/// Domain event raised when a user logs in.
/// </summary>
public readonly record struct UserLoggedInEvent(UserId UserId) : IDomainEvent;
```

### 4.2. Auth Errors

**File:** `Auth/Errors/AuthErrors.cs`

```csharp
namespace Menlo.Lib.Auth.Errors;

using Menlo.Lib.Common.Abstractions;

public sealed class AuthError(string code, string message) : Error(code, message);

public static class AuthErrors
{
    public static AuthError UserNotFound(string externalId) =>
        new("Auth.UserNotFound", $"No user found with external ID: {externalId}");

    public static AuthError Unauthorized() =>
        new("Auth.Unauthorized", "You are not authorized to perform this action.");

    public static AuthError Forbidden(string policy) =>
        new("Auth.Forbidden", $"Access denied. Required policy: {policy}");
}
```

### 4.3. Value Objects

**File:** `Auth/ValueObjects/UserId.cs`

```csharp
namespace Menlo.Lib.Auth.ValueObjects;

public readonly record struct UserId(Guid Value);
```

**File:** `Auth/ValueObjects/ExternalUserId.cs`

```csharp
namespace Menlo.Lib.Auth.ValueObjects;

/// <summary>
/// Represents the Entra ID Object ID (oid claim).
/// </summary>
public readonly record struct ExternalUserId(string Value);
```

**File:** `Auth/Models/UserProfile.cs`

```csharp
namespace Menlo.Lib.Auth.Models;

/// <summary>
/// DTO returned by the /auth/user endpoint.
/// Does not expose any IdP-specific information.
/// </summary>
public sealed record UserProfile(
    string Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles
);
```

---

## 5. Frontend Implementation (Angular)

### 5.1. Auth Service

**File:** `core/auth/auth.service.ts`

```typescript
import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { UserProfile } from './auth.models';
import { catchError, tap, of, firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // State signals
  private readonly userSignal = signal<UserProfile | null>(null);
  private readonly loadingSignal = signal<boolean>(true);
  private readonly errorSignal = signal<string | null>(null);

  // Public computed signals
  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);
  readonly isLoading = this.loadingSignal.asReadonly();
  readonly roles = computed(() => this.userSignal()?.roles ?? []);

  /**
   * Fetches the current user from the backend.
   * Should be called on app initialization.
   */
  async loadUser(): Promise<void> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    try {
      const user = await firstValueFrom(
        this.http.get<UserProfile>(`${environment.apiUrl}/auth/user`).pipe(
          catchError(() => of(null))
        )
      );
      this.userSignal.set(user);
    } finally {
      this.loadingSignal.set(false);
    }
  }

  /**
   * Redirects to backend login endpoint.
   * The backend handles OIDC flow.
   */
  login(returnUrl?: string): void {
    const encodedReturnUrl = encodeURIComponent(returnUrl ?? this.router.url);
    // Redirect to backend login - backend handles OIDC
    window.location.href = `${environment.apiUrl}/auth/login?returnUrl=${encodedReturnUrl}`;
  }

  /**
   * Posts to backend logout endpoint.
   */
  async logout(): Promise<void> {
    await firstValueFrom(
      this.http.post(`${environment.apiUrl}/auth/logout`, {})
    );
    this.userSignal.set(null);
    // Redirect will be handled by backend OIDC signout
  }

  /**
   * Checks if the user has at least one of the specified roles.
   */
  hasRole(...requiredRoles: string[]): boolean {
    const userRoles = this.roles();
    return requiredRoles.some(role => userRoles.includes(role));
  }
}
```

### 5.2. HTTP Interceptor

**File:** `core/auth/auth.interceptor.ts`

```typescript
import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Only add credentials for requests to our API
  if (req.url.startsWith(environment.apiUrl)) {
    const clonedRequest = req.clone({
      withCredentials: true
    });
    return next(clonedRequest);
  }

  return next(req);
};
```

### 5.3. Auth Guards

**File:** `core/auth/auth.guard.ts`

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Wait for user to load if still loading
  if (authService.isLoading()) {
    await authService.loadUser();
  }

  if (authService.isAuthenticated()) {
    return true;
  }

  // Redirect to login
  authService.login(router.url);
  return false;
};
```

**File:** `core/auth/role.guard.ts`

```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from './auth.service';

export const roleGuard: CanActivateFn = async (route: ActivatedRouteSnapshot) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Wait for user to load if still loading
  if (authService.isLoading()) {
    await authService.loadUser();
  }

  if (!authService.isAuthenticated()) {
    authService.login(router.url);
    return false;
  }

  const requiredRoles = route.data['roles'] as string[] | undefined;
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }

  if (authService.hasRole(...requiredRoles)) {
    return true;
  }

  // Redirect to unauthorized page
  return router.createUrlTree(['/unauthorized']);
};
```

### 5.4. User Context Component

**File:** `core/auth/auth.models.ts`

```typescript
export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export const MenloRoles = {
  Admin: 'Menlo.Admin',
  User: 'Menlo.User',
  Reader: 'Menlo.Reader'
} as const;
```

**App Initialization (in `app.config.ts`):**

```typescript
import { APP_INITIALIZER } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { AuthService } from './core/auth/auth.service';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig = {
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () => authService.loadUser(),
      deps: [AuthService],
      multi: true
    }
  ]
};
```

---

## 6. Testing Strategy

### 6.1. Backend Test Authentication Handler

**File:** `Fixtures/TestAuthHandler.cs`

```csharp
namespace Menlo.Api.Tests.Fixtures;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptionsMonitor<TestAuthHandlerOptions> testOptions,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestScheme";
    public const string DefaultUserId = "test-user-id";
    public const string DefaultEmail = "test@example.com";
    public const string DefaultName = "Test User";

    private readonly TestAuthHandlerOptions _testOptions = testOptions.CurrentValue;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_testOptions.SimulateUnauthenticated)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, DefaultUserId),
            new(ClaimTypes.Email, DefaultEmail),
            new(ClaimTypes.Name, DefaultName),
        ];

        claims.AddRange(_testOptions.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        ClaimsIdentity identity = new(claims, SchemeName);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

**File:** `Fixtures/AuthenticatedWebApplicationFactory.cs`

```csharp
namespace Menlo.Api.Tests.Fixtures;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

public sealed class AuthenticatedWebApplicationFactory : WebApplicationFactory<Program>
{
    public string[] UserRoles { get; init; } = ["Menlo.User"];
    public bool SimulateUnauthenticated { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Override authentication with test handler
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, options => { });

            // Configure the test handler's behaviour for this factory instance
            services.Configure<TestAuthHandlerOptions>(options =>
            {
                options.Roles = UserRoles;
                options.SimulateUnauthenticated = SimulateUnauthenticated;
            });
        });
    }
}
```

**File:** `Fixtures/TestAuthHandlerOptions.cs`

```csharp
namespace Menlo.Api.Tests.Fixtures;

public sealed class TestAuthHandlerOptions
{
    public string[] Roles { get; set; } = ["Menlo.User"];
    public bool SimulateUnauthenticated { get; set; }
}
```

### 6.2. Backend Integration Tests

Test naming follows the pattern: `GivenSomething_WhenSomeCondition_AndOrSomeOptionalCondition`.
All assertions are wrapped in well-named expectation helper methods.

**File:** `Auth/UserEndpointTests.cs`

```csharp
namespace Menlo.Api.Tests.Auth;

using Menlo.Api.Tests.Fixtures;
using Menlo.Lib.Auth.Models;
using System.Net;
using System.Net.Http.Json;

public sealed class UserEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;

    public UserEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.UserRoles = ["Menlo.User"];
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenGettingUserProfile()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("/auth/user");
        UserProfile? profile = await response.Content.ReadFromJsonAsync<UserProfile>();

        ItShouldReturnOk(response);
        ItShouldReturnValidUserProfile(profile);
        ItShouldContainExpectedEmail(profile);
        ItShouldContainUserRole(profile);
    }

    [Fact]
    public async Task GivenUnauthenticatedUser_WhenGettingUserProfile()
    {
        AuthenticatedWebApplicationFactory unauthFactory = new() { SimulateUnauthenticated = true };
        HttpClient client = unauthFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/auth/user");

        ItShouldReturnUnauthorized(response);
    }

    private static void ItShouldReturnOk(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static void ItShouldReturnUnauthorized(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static void ItShouldReturnValidUserProfile(UserProfile? profile)
    {
        profile.ShouldNotBeNull();
        profile.Id.ShouldNotBeNullOrWhiteSpace();
        profile.DisplayName.ShouldNotBeNullOrWhiteSpace();
    }

    private static void ItShouldContainExpectedEmail(UserProfile? profile)
    {
        profile.ShouldNotBeNull();
        profile.Email.ShouldBe(TestAuthHandler.DefaultEmail);
    }

    private static void ItShouldContainUserRole(UserProfile? profile)
    {
        profile.ShouldNotBeNull();
        profile.Roles.ShouldContain("Menlo.User");
    }
}

public sealed class PolicyAuthorizationTests
{
    [Fact]
    public async Task GivenUserWithReaderRole_WhenAccessingAdminEndpoint()
    {
        AuthenticatedWebApplicationFactory factory = new() { UserRoles = ["Menlo.Reader"] };
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/admin/settings");

        ItShouldReturnForbidden(response);
    }

    [Fact]
    public async Task GivenUserWithAdminRole_WhenAccessingAdminEndpoint()
    {
        AuthenticatedWebApplicationFactory factory = new() { UserRoles = ["Menlo.Admin"] };
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/admin/settings");

        ItShouldReturnOk(response);
    }

    [Fact]
    public async Task GivenUserWithUserRole_WhenAccessingBudgetEditEndpoint()
    {
        AuthenticatedWebApplicationFactory factory = new() { UserRoles = ["Menlo.User"] };
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/budgets", null);

        ItShouldReturnOk(response);
    }

    [Fact]
    public async Task GivenUserWithReaderRole_WhenAccessingBudgetEditEndpoint()
    {
        AuthenticatedWebApplicationFactory factory = new() { UserRoles = ["Menlo.Reader"] };
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/api/budgets", null);

        ItShouldReturnForbidden(response);
    }

    private static void ItShouldReturnOk(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static void ItShouldReturnForbidden(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
```

### 6.3. Angular Unit Tests

Angular tests use Jasmine's `describe/it` pattern. Organise tests using nested `describe` blocks
for the Given/When structure and use helper functions for assertions.

**File:** `core/auth/auth.service.spec.ts`

```typescript
import { TestBed } from '@angular/core/testing';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { UserProfile } from './auth.models';

describe('AuthService', () => {
  let authService: AuthService;
  let httpMock: HttpTestingController;

  const mockUser: UserProfile = {
    id: '123',
    email: 'test@example.com',
    displayName: 'Test User',
    roles: ['Menlo.User']
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService
      ]
    });
    authService = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // Helper assertion functions
  const itShouldBeAuthenticated = (): void => {
    expect(authService.isAuthenticated()).toBeTrue();
  };

  const itShouldNotBeAuthenticated = (): void => {
    expect(authService.isAuthenticated()).toBeFalse();
  };

  const itShouldHaveUser = (expected: UserProfile): void => {
    expect(authService.user()).toEqual(expected);
  };

  const itShouldHaveNoUser = (): void => {
    expect(authService.user()).toBeNull();
  };

  const itShouldContainRole = (role: string): void => {
    expect(authService.roles()).toContain(role);
  };

  const itShouldNotBeLoading = (): void => {
    expect(authService.isLoading()).toBeFalse();
  };

  describe('given a successful user response', () => {
    describe('when loading user', () => {
      it('should set user and authentication state', async () => {
        const loadPromise = authService.loadUser();

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/user`);
        expect(req.request.method).toBe('GET');
        req.flush(mockUser);

        await loadPromise;

        itShouldHaveUser(mockUser);
        itShouldBeAuthenticated();
        itShouldContainRole('Menlo.User');
        itShouldNotBeLoading();
      });
    });
  });

  describe('given an unauthenticated response', () => {
    describe('when loading user', () => {
      it('should clear user and authentication state', async () => {
        const loadPromise = authService.loadUser();

        const req = httpMock.expectOne(`${environment.apiUrl}/auth/user`);
        req.flush(null, { status: 401, statusText: 'Unauthorized' });

        await loadPromise;

        itShouldHaveNoUser();
        itShouldNotBeAuthenticated();
        itShouldNotBeLoading();
      });
    });
  });

  describe('given a user with multiple roles', () => {
    const adminUser: UserProfile = {
      ...mockUser,
      roles: ['Menlo.Admin', 'Menlo.User']
    };

    describe('when checking roles', () => {
      beforeEach(async () => {
        const loadPromise = authService.loadUser();
        httpMock.expectOne(`${environment.apiUrl}/auth/user`).flush(adminUser);
        await loadPromise;
      });

      it('should return true for assigned roles', () => {
        expect(authService.hasRole('Menlo.Admin')).toBeTrue();
        expect(authService.hasRole('Menlo.User')).toBeTrue();
      });

      it('should return false for unassigned roles', () => {
        expect(authService.hasRole('Menlo.Reader')).toBeFalse();
      });

      it('should return true when any role matches', () => {
        expect(authService.hasRole('Menlo.Admin', 'Menlo.Reader')).toBeTrue();
      });
    });
  });
});
```

---

## 7. Security Considerations

1. **No Tenant/Client ID Exposure:** The Angular frontend never receives the Entra Tenant ID or Client ID. All OIDC configuration is server-side.

2. **Cookie Security:**
   - `HttpOnly`: Prevents JavaScript access to session cookie.
   - `Secure`: Cookie only sent over HTTPS.
   - `SameSite=Strict`: Prevents CSRF attacks.
   - `Domain`: Scoped to shared root domain.

3. **CSRF Protection:** `SameSite=Strict` combined with `POST` for logout provides CSRF protection.

4. **Token Storage:** Access/Refresh tokens are stored server-side only; never exposed to browser.

5. **Input Validation:** `returnUrl` parameter validated to prevent open redirect attacks.

6. **API Security:** All `/api/*` endpoints require authentication by default.

---

## 8. Implementation Checklist

### Backend Tasks

- [ ] Add NuGet packages to `Menlo.Api.csproj`
- [ ] Create `Auth/Options/MenloAuthOptions.cs`
- [ ] Create `Auth/Options/MenloAuthOptionsValidator.cs`
- [ ] Create `Auth/Policies/MenloPolicies.cs`
- [ ] Create `Auth/Policies/AuthPoliciesExtensions.cs`
- [ ] Create `Auth/Endpoints/AuthEndpoints.cs`
- [ ] Create `Auth/Endpoints/LoginEndpoint.cs`
- [ ] Create `Auth/Endpoints/LogoutEndpoint.cs`
- [ ] Create `Auth/Endpoints/UserEndpoint.cs`
- [ ] Create `Auth/Security/SecurityHeadersExtensions.cs`
- [ ] Create `Auth/AuthServiceCollectionExtensions.cs`
- [ ] Update `Program.cs` to wire up authentication and security headers
- [ ] Create `appsettings.json` AzureAd section (with placeholders)
- [ ] Configure User Secrets for development

### Library Tasks

- [ ] Create `Auth/Entities/User.cs`
- [ ] Create `Auth/Events/UserLoggedInEvent.cs`
- [ ] Create `Auth/Errors/AuthErrors.cs`
- [ ] Create `Auth/ValueObjects/UserId.cs`
- [ ] Create `Auth/ValueObjects/ExternalUserId.cs`
- [ ] Create `Auth/Models/UserProfile.cs`

### Frontend Tasks

- [ ] Create `core/auth/auth.models.ts`
- [ ] Create `core/auth/auth.service.ts`
- [ ] Create `core/auth/auth.interceptor.ts`
- [ ] Create `core/auth/auth.guard.ts`
- [ ] Create `core/auth/role.guard.ts`
- [ ] Create `core/auth/index.ts` barrel export
- [ ] Update `app.config.ts` to register interceptor and initializer
- [ ] Add `apiUrl` to environment files

### Testing Tasks

- [ ] Create `Fixtures/TestAuthHandler.cs`
- [ ] Create `Fixtures/TestAuthHandlerOptions.cs`
- [ ] Create `Fixtures/AuthenticatedWebApplicationFactory.cs`
- [ ] Create `Auth/UserEndpointTests.cs`
- [ ] Create `Auth/PolicyAuthorizationTests.cs`
- [ ] Create Angular `auth.service.spec.ts`
- [ ] Create Angular `auth.guard.spec.ts`

### Configuration Tasks

- [ ] Configure Entra ID App Registration (manual)
- [ ] Set up User Secrets for local development
- [ ] Configure production environment variables
- [ ] Update Cloudflare DNS records (manual)
