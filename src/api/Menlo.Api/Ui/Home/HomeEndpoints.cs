using Menlo.Api.Ui.Shared;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Menlo.Api.Ui.Home;

internal static class HomeEndpoints
{
    public static IEndpointRouteBuilder MapHomePage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", Page);
        return app;
    }

    private static RazorComponentResult<HomePage> Page(HttpContext context) =>
        Render.Page<HomePage>(context, HomePageModel.Create());
}
