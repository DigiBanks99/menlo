using Menlo.Ui.Components;

namespace Menlo.Ui.Layouts;

/// <summary>
/// One destination in the app's primary navigation.
/// </summary>
/// <param name="Label">The text shown.</param>
/// <param name="Href">The address. "/" matches only the home page; anything else also matches its sub-pages.</param>
/// <param name="Icon">The icon shown with the label.</param>
public sealed record MnlNavItem(string Label, string Href, MnlIconName Icon)
{
    /// <summary>
    /// Whether this item is the current section for the given request path.
    /// </summary>
    public bool IsCurrent(string path) =>
        Href == "/"
            ? path is "/" or ""
            : path.Equals(Href, StringComparison.OrdinalIgnoreCase)
              || path.StartsWith(Href + "/", StringComparison.OrdinalIgnoreCase);
}
