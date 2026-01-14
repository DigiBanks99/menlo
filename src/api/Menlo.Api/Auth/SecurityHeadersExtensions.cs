using NetEscapades.AspNetCore.SecurityHeaders;

namespace Menlo.Api.Auth;

/// <summary>
/// Extension methods for configuring security headers.
/// </summary>
internal static class SecurityHeadersExtensions
{
    extension(WebApplication app)
    {
        internal WebApplication UseMenloSecurityHeaders()
        {
            HeaderPolicyCollection securityPolicy = new HeaderPolicyCollection()
                .AddMenloSecurityHeaders()
                .AddCrossOriginOpenerPolicy(policyBuilder => policyBuilder.UnsafeNone());

            app.UseSecurityHeaders(securityPolicy);

            return app;
        }
    }

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
            builder.AddScriptSrc()
                .Self()
                .UnsafeInline(); // for Scalar
            builder.AddStyleSrc()
                .Self()
                .UnsafeInline(); // for Scalar
            builder.AddImgSrc().Self();
            builder.AddFontSrc().Self().From("https://fonts.scalar.com");
            builder.AddConnectSrc().Self().From("https://login.microsoftonline.com");

            builder.AddDefaultSrc().None();
            builder.AddFrameAncestors().None();
            builder.AddFormAction().Self();
        });

        return policies;
    }
}
