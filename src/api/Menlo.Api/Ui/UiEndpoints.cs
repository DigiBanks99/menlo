using Menlo.Api.Auth.Policies;
using Menlo.Api.Ui.Analytics;
using Menlo.Api.Ui.Budgets;
using Menlo.Api.Ui.Design;
using Menlo.Api.Ui.Errors;
using Menlo.Api.Ui.Home;
using Menlo.Api.Ui.Shared;
using Menlo.Api.Ui.SignIn;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Menlo.Api.Ui;

/// <summary>
/// Registers and maps the server-rendered HTML UI.
/// </summary>
internal static class UiEndpoints
{
    public static IHostApplicationBuilder AddMenloUi(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents();
        builder.Services.AddScoped<BudgetUi>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, UiAuthorizationResultHandler>();
        builder.Services.TryAddSingleton(TimeProvider.System);
        return builder;
    }

    public static WebApplication MapMenloUi(this WebApplication app)
    {
        // Every UI endpoint is marked, signed-in only, and hidden from the OpenAPI document
        RouteGroupBuilder ui = app
            .MapGroup(string.Empty)
            .WithMetadata(UiEndpointMetadata.Instance)
            .RequireAuthorization(MenloPolicies.RequireAuthenticated)
            .DisableAntiforgery()
            .ExcludeFromDescription();

        ui.MapSignInPage();
        ui.MapHomePage();
        ui.MapBudgetPages();
        ui.MapAnalyticsPage();

        if (DesignEndpoints.IsEnabled(app))
        {
            ui.MapDesignGallery();
        }

        // Unknown API routes and well-known probes get a bare 404, not a page
        app.Map("/api/{**path}", () => Results.NotFound()).ExcludeFromDescription();
        app.Map("/.well-known/{**path}", () => Results.NotFound()).ExcludeFromDescription();

        // Anything else that asks for a page gets the not-found page. GET only: other methods on unknown
        // paths must stay unrouted, because the sign-in callback (POST /signin-oidc) is handled by the
        // authentication middleware, not by an endpoint, and must not pick up the UI's anti-forgery check.
        ui.MapFallback(context =>
                Render.Page<NotFoundPage>(context, statusCode: StatusCodes.Status404NotFound).ExecuteAsync(context))
            .WithMetadata(new HttpMethodMetadata([HttpMethods.Get]));

        return app;
    }
}
