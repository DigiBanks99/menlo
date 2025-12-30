# Tutorial: Creating a Secure Minimal API Endpoint

This tutorial guides you through creating a new Minimal API endpoint secured with Menlo's authorization policies.

## Prerequisites

- A working Menlo development environment.
- Understanding of [Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis).
- Familiarity with [Menlo Authentication API](../reference/authentication-api.md).

## Step 1: Define the Endpoint

Create a new endpoint class in your feature slice. For example, `GetBudgetSummaryEndpoint.cs`.

```csharp
using Menlo.Api.Auth.Policies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Menlo.Api.Features.Budget;

public static class GetBudgetSummaryEndpoint
{
    public static void MapGetBudgetSummary(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/budget/summary", HandleAsync)
            .WithName("GetBudgetSummary")
            .WithTags("Budget")
            .RequireAuthorization(MenloPolicies.CanViewBudget); // <--- Apply Policy
    }

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        // Implementation...
        return Results.Ok(new { Total = 1000 });
    }
}
```

## Step 2: Apply Authorization

Use the `.RequireAuthorization()` extension method with a constant from `MenloPolicies`.

```csharp
.RequireAuthorization(MenloPolicies.CanViewBudget)
```

This ensures that only users satisfying the `CanViewBudget` policy (Admins, Users, Readers) can access this endpoint.

## Step 3: Accessing User Information

If you need to access the current user's ID or claims, inject `ClaimsPrincipal` or use `HttpContext`.

```csharp
private static async Task<IResult> HandleAsync(
    ClaimsPrincipal user,
    IBudgetService service,
    CancellationToken cancellationToken)
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    // ...
}
```

## Step 4: Register the Endpoint

Ensure your endpoint mapping method is called in `Program.cs` or your module's registration extension.

```csharp
// In Program.cs or ServiceCollectionExtensions
app.MapGetBudgetSummary();
```

## Testing

1. **Unauthenticated**: Call the endpoint without a cookie. Expect `401 Unauthorized`.
2. **Unauthorized**: Call with a user who lacks the required role. Expect `403 Forbidden`.
3. **Authorized**: Call with a valid user. Expect `200 OK`.

## Summary

You have successfully created a secured API endpoint using Menlo's policy-based authorization system.
