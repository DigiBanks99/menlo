using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Menlo.Api.Antiforgery;

internal static class MenloAntiforgeryExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddMenloAntiforgery()
        {
            builder.Services.AddAntiforgery(options =>
            {
                options.Cookie.Name = MenloAntiforgeryDefaults.CookieTokenName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.IsEssential = true;
                options.HeaderName = MenloAntiforgeryDefaults.HeaderName;
            });

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseMenloAntiforgery()
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.IsHttps && ShouldEmitRequestToken(context.Request))
                {
                    IAntiforgery antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);

                    if (!string.IsNullOrWhiteSpace(tokens.RequestToken))
                    {
                        context.Response.Cookies.Append(
                            MenloAntiforgeryDefaults.RequestTokenCookieName,
                            tokens.RequestToken,
                            BuildRequestTokenCookieOptions());
                    }
                }

                if (context.Request.IsHttps && RequiresValidation(context.Request))
                {
                    IAntiforgery antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();

                    try
                    {
                        await antiforgery.ValidateRequestAsync(context);
                    }
                    catch (AntiforgeryValidationException)
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;

                        IProblemDetailsService problemDetailsService =
                            context.RequestServices.GetRequiredService<IProblemDetailsService>();

                        await problemDetailsService.WriteAsync(new ProblemDetailsContext
                        {
                            HttpContext = context,
                            ProblemDetails = new ProblemDetails
                            {
                                Status = StatusCodes.Status400BadRequest,
                                Title = "Invalid anti-forgery token.",
                                Detail = $"Send the {MenloAntiforgeryDefaults.HeaderName} header with the request token cookie."
                            }
                        });

                        return;
                    }
                }

                await next(context);
            });

            return app;
        }
    }

    private static bool RequiresValidation(HttpRequest request) =>
        request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
        && (HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsDelete(request.Method));

    private static bool ShouldEmitRequestToken(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/auth", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!(HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method)
            || HttpMethods.IsTrace(request.Method)))
        {
            return false;
        }

        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || request.Headers.Accept.Any(accept => accept?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true)
            || !Path.HasExtension(request.Path.Value);
    }

    private static CookieOptions BuildRequestTokenCookieOptions() => new()
    {
        HttpOnly = false,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        IsEssential = true
    };
}

internal static class MenloAntiforgeryDefaults
{
    public const string CookieTokenName = ".Menlo.Antiforgery";
    public const string RequestTokenCookieName = "XSRF-TOKEN";
    public const string HeaderName = "X-XSRF-TOKEN";
}
