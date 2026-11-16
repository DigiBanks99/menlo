using Menlo.Api.Auth.Policies;
using Menlo.Api.Ui.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Menlo.Api.Ui.Analytics;

internal static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsPage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/analytics", Page).RequireAuthorization(MenloPolicies.CanViewBudget);
        return app;
    }

    private static RazorComponentResult<AnalyticsPage> Page(HttpContext context, string? period = null) =>
        Render.Page<AnalyticsPage>(context, AnalyticsPageModel.Create(period));
}
