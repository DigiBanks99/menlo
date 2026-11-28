namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The htmx attributes of links that change the budget page's state but not its year, such as opening a side rail
/// section. They keep their real address and swap only the budget frame.
/// </summary>
internal static class BudgetRegion
{
    /// <summary>The id of the region the links swap: the side rail and the grid.</summary>
    public const string Id = "budget-frame";

    private const string Selector = "#" + Id;

    /// <summary>Loads an address and swaps the budget frame for the one in the answer.</summary>
    public static IReadOnlyDictionary<string, object> GoTo(string url) => new Dictionary<string, object>
    {
        ["hx-get"] = url,
        ["hx-target"] = Selector,
        ["hx-select"] = Selector,
        ["hx-swap"] = "outerHTML",
        ["hx-replace-url"] = "true",
        ["hx-sync"] = $"{Selector}:replace"
    };
}
