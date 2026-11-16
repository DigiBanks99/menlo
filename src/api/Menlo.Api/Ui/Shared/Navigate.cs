using System.Text.Json;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// Sends the browser to another page after an action. An htmx request gets an <c>HX-Location</c>
/// header, which loads the page without a full reload; anything else gets an ordinary redirect.
/// Pair it with <see cref="Flash"/> to show a toast on the page it lands on.
/// </summary>
internal static class Navigate
{
    public static IResult To(HttpContext context, string url)
    {
        if (!context.Request.Headers.ContainsKey("HX-Request"))
        {
            return Results.LocalRedirect(url);
        }

        context.Response.Headers["HX-Location"] = JsonSerializer.Serialize(new { path = url });
        return Results.NoContent();
    }
}
