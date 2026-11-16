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
            HeaderPolicyCollection strict = BuildPolicy(allowInline: false);

            if (!app.Environment.IsDevelopment())
            {
                app.UseSecurityHeaders(strict);
                return app;
            }

            // The API reference (Scalar) is only mapped in Development and needs inline scripts and styles.
            // Everything else, the UI included, gets the same strict policy as production, so a page that
            // breaks the policy fails in development first.
            HeaderPolicyCollection apiReference = BuildPolicy(allowInline: true);

            app.UseWhen(IsApiReference, branch => branch.UseSecurityHeaders(apiReference));
            app.UseWhen(context => !IsApiReference(context), branch => branch.UseSecurityHeaders(strict));

            return app;
        }
    }

    /// <summary>
    /// Adds Menlo's security headers with the strict content security policy.
    /// </summary>
    /// <param name="policies">The header policy collection.</param>
    /// <returns>The header policy collection for chaining.</returns>
    public static HeaderPolicyCollection AddMenloSecurityHeaders(this HeaderPolicyCollection policies) =>
        policies.AddMenloSecurityHeaders(allowInline: false);

    private static bool IsApiReference(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
        || context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);

    private static HeaderPolicyCollection BuildPolicy(bool allowInline) =>
        new HeaderPolicyCollection()
            .AddMenloSecurityHeaders(allowInline)
            .AddCrossOriginOpenerPolicy(policyBuilder => policyBuilder.UnsafeNone());

    private static HeaderPolicyCollection AddMenloSecurityHeaders(this HeaderPolicyCollection policies, bool allowInline)
    {
        policies.AddDefaultApiSecurityHeaders();

        policies.AddStrictTransportSecurityMaxAgeIncludeSubDomains(maxAgeInSeconds: (int)TimeSpan.FromDays(365 * 2).TotalSeconds);

        policies.AddContentSecurityPolicy(builder =>
        {
            // The UI loads every script and stylesheet from this origin and uses no inline script,
            // no inline style and no eval, so 'self' is all it needs.
            // Each AddScriptSrc() or AddStyleSrc() call replaces the previous directive, so each is one chain.
            if (allowInline)
            {
                builder.AddScriptSrc().Self().UnsafeInline();
                builder.AddStyleSrc().Self().UnsafeInline();
            }
            else
            {
                builder.AddScriptSrc().Self();
                builder.AddStyleSrc().Self();
            }

            builder.AddImgSrc().Self().Data();
            builder.AddFontSrc().Self().From("https://fonts.scalar.com");
            builder.AddConnectSrc().Self();

            builder.AddDefaultSrc().None();
            builder.AddFrameAncestors().None();
            // Signing out is a form post that redirects to Microsoft, and browsers apply form-action to the redirect too
            builder.AddFormAction().Self().From("https://login.microsoftonline.com");
        });

        return policies;
    }
}
