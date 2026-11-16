using Menlo.Ui.Components;
using Menlo.Ui.Utilities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// Builds the htmx response headers that tell the browser what happened: show a toast, close the open
/// panel, or raise an event that other fragments listen for so they reload themselves.
/// </summary>
/// <remarks>
/// A fragment reloads on an event with <c>hx-trigger="items-changed from:body"</c>. That keeps the
/// endpoint that changed the data from knowing which parts of the page show it.
/// </remarks>
internal sealed class HxResponse
{
    private readonly JsonObject _events = [];

    /// <summary>Shows a toast.</summary>
    public HxResponse Toast(string message, MnlTone tone = MnlTone.Success)
    {
        _events[MnlToastMessage.EventName] = new JsonObject
        {
            ["message"] = message,
            ["tone"] = Css.Token(tone)
        };
        return this;
    }

    /// <summary>Closes the open panel.</summary>
    public HxResponse ClosePanel()
    {
        _events[MnlPanel.CloseEvent] = true;
        return this;
    }

    /// <summary>Raises a named event on the page body. See <see cref="UiEvents"/> for the names.</summary>
    public HxResponse Raise(string eventName)
    {
        _events[eventName] = true;
        return this;
    }

    /// <summary>
    /// Writes the headers and returns 204 No Content: nothing is swapped, the events do the work.
    /// </summary>
    public IResult NoContent(HttpContext context)
    {
        Apply(context.Response);
        return Results.NoContent();
    }

    /// <summary>
    /// Writes the headers onto a response that will also carry a fragment.
    /// </summary>
    public void Apply(HttpResponse response)
    {
        if (_events.Count > 0)
        {
            response.Headers["HX-Trigger"] = _events.ToJsonString(JsonSerializerOptions.Default);
        }
    }
}

/// <summary>
/// Names of the events that fragments listen for. Raise one after changing the data it names.
/// </summary>
internal static class UiEvents
{
    /// <summary>A budget's categories were created, changed, deleted or restored.</summary>
    public const string CategoriesChanged = "categories-changed";

    /// <summary>A category's items were created, changed or deleted.</summary>
    public const string ItemsChanged = "items-changed";
}

/// <summary>
/// Reads the htmx request headers.
/// </summary>
internal static class HxRequest
{
    /// <summary>
    /// Whether the request asks for a fragment: sent by htmx, and not a boosted page navigation.
    /// </summary>
    public static bool IsFragment(HttpRequest request) =>
        request.Headers.ContainsKey("HX-Request") && !request.Headers.ContainsKey("HX-Boosted");
}
