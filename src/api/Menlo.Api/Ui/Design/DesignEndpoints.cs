using Menlo.Api.Ui.Shared;
using Menlo.Ui.Components;
using Menlo.Ui.Gallery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Menlo.Api.Ui.Design;

/// <summary>
/// The design-system gallery at <c>/design</c>. It shows no household data, so it needs no sign-in.
/// It is mapped in Development, or anywhere <c>Features:DesignGallery</c> is true.
/// </summary>
internal static class DesignEndpoints
{
    public static IEndpointRouteBuilder MapDesignGallery(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder design = app.MapGroup("/design").AllowAnonymous();

        design.MapGet("/", (HttpContext context) => Render.Page<DesignPage>(context));
        design.MapGet("/panel", Panel);
        design.MapPost("/toast", Toast);

        return app;
    }

    public static bool IsEnabled(WebApplication app) =>
        app.Configuration.GetValue("Features:DesignGallery", defaultValue: app.Environment.IsDevelopment());

    private static RazorComponentResult<GalleryPanelExample> Panel(string? mode = null) =>
        new(new Dictionary<string, object?>
        {
            ["Mode"] = string.Equals(mode, "dialog", StringComparison.OrdinalIgnoreCase)
                ? MnlPanelMode.Dialog
                : MnlPanelMode.Auto
        });

    private static IResult Toast(HttpContext context, string? tone = null) =>
        string.Equals(tone, "error", StringComparison.OrdinalIgnoreCase)
            ? new HxResponse().Toast("That didn't work. Nothing was changed.", MnlTone.Error).NoContent(context)
            : new HxResponse().Toast("Saved.").NoContent(context);
}
