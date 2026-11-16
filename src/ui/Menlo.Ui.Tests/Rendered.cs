using AngleSharp;
using AngleSharp.Html.Dom;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Menlo.Ui.Tests;

/// <summary>
/// Renders a component to HTML the way the server does, and parses the result so tests can query it.
/// </summary>
internal static class Rendered
{
    /// <summary>
    /// Renders a component with the given parameters.
    /// </summary>
    public static async Task<IHtmlDocument> ComponentAsync<TComponent>(IDictionary<string, object?>? parameters = null)
        where TComponent : IComponent
    {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

        string html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            ParameterView view = ParameterView.FromDictionary(parameters ?? new Dictionary<string, object?>());
            return (await renderer.RenderComponentAsync<TComponent>(view)).ToHtmlString();
        });

        IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        return (IHtmlDocument)await context.OpenAsync(request => request.Content(html));
    }

    /// <summary>
    /// A child-content fragment holding plain text.
    /// </summary>
    public static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

    /// <summary>
    /// The first element matching a selector. Fails the test when there is none.
    /// </summary>
    public static IElement Find(this IParentNode node, string selector) =>
        node.QuerySelector(selector) ?? throw new ShouldAssertException($"No element matches '{selector}'.");
}
