using Microsoft.AspNetCore.Authorization;

namespace Menlo.Api.Auth.Policies;

/// <summary>
/// Extension methods for configuring authorization policies.
/// </summary>
public static class AuthPoliciesExtensions
{
    /// <summary>
    /// Adds Menlo application authorization policies.
    /// </summary>
    /// <param name="builder">The authorization builder.</param>
    /// <returns>The authorization builder for chaining.</returns>
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
