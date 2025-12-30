using NetEscapades.AspNetCore.SecurityHeaders;

namespace Menlo.Api.Auth;

/// <summary>
/// Extension methods for configuring security headers.
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds Menlo-specific security headers configuration.
    /// </summary>
    /// <param name="policies">The header policy collection.</param>
    /// <returns>The header policy collection for chaining.</returns>
    public static HeaderPolicyCollection AddMenloSecurityHeaders(this HeaderPolicyCollection policies)
    {
        policies.AddDefaultApiSecurityHeaders();

        policies.AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: (int)TimeSpan.FromDays(365 * 2).TotalSeconds);

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
