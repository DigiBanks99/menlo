namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The budget detail page keeps its state in the URL and has one refreshable region. A link that
/// changes the state asks for the page at the new address and swaps that region in place, so the
/// rest of the page (and the scroll position) stays put.
/// </summary>
internal static class BudgetRegion
{
    /// <summary>The id of the region.</summary>
    public const string Id = "budget-detail";

    private const string Selector = "#" + Id;

    // Every request that swaps the region is synchronised on the region, and a newer one replaces an
    // older one still in flight. Without this, a reload that started just before someone typed in the
    // search box could finish after their request and put the old state back.
    private const string Sync = Selector + ":replace";

    /// <summary>The hx-sync value for anything that swaps the region.</summary>
    public const string SyncValue = Sync;

    /// <summary>
    /// Attributes for a link or control that moves the page to another state.
    /// The plain href still works without script.
    /// </summary>
    public static Dictionary<string, object> GoTo(string url) => new()
    {
        ["hx-get"] = url,
        ["hx-target"] = Selector,
        ["hx-select"] = Selector,
        ["hx-swap"] = "outerHTML",
        ["hx-replace-url"] = "true",
        ["hx-sync"] = Sync
    };

    /// <summary>
    /// Attributes for the region itself: reload at the current address when budget data changes.
    /// An empty hx-get means "the address in the address bar".
    /// </summary>
    public static Dictionary<string, object> ReloadOnChange() => new()
    {
        ["hx-get"] = string.Empty,
        ["hx-trigger"] = $"{Shared.UiEvents.CategoriesChanged} from:body, {Shared.UiEvents.ItemsChanged} from:body",
        ["hx-select"] = Selector,
        ["hx-swap"] = "outerHTML",
        ["hx-sync"] = Sync
    };
}
