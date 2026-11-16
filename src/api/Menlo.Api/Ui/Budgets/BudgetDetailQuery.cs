using Microsoft.AspNetCore.Http.Extensions;
using System.Globalization;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The state of the budget detail page, all of it in the query string: which category's items are
/// open, the category tree filters, and the summary's period. Because the state is in the URL, a
/// region of the page refreshes by requesting the page again and taking its own part of the answer,
/// and a reload or a shared link shows the same thing.
/// </summary>
/// <param name="Category">The leaf category whose items workspace is open.</param>
/// <param name="Search">Text the category names must contain.</param>
/// <param name="Flow">Show only categories with this budget flow.</param>
/// <param name="Attribution">Show only categories with this attribution.</param>
/// <param name="Deleted">Whether deleted categories are shown.</param>
/// <param name="View">The summary period: "monthly" or "yearly".</param>
/// <param name="Month">The month of a monthly summary, 1 to 12.</param>
public sealed record BudgetDetailQuery(
    Guid? Category,
    string Search,
    string Flow,
    string Attribution,
    bool Deleted,
    string View,
    int Month)
{
    public const string Monthly = "monthly";
    public const string Yearly = "yearly";

    /// <summary>Whether the summary covers one month.</summary>
    public bool IsMonthly => View == Monthly;

    /// <summary>Whether any category filter is set.</summary>
    public bool HasFilter => Search.Length > 0 || Flow.Length > 0 || Attribution.Length > 0;

    /// <summary>
    /// Reads the state from a request, falling back to defaults for anything missing or invalid.
    /// </summary>
    public static BudgetDetailQuery From(IQueryCollection query, int currentMonth)
    {
        Guid? category = Guid.TryParse(query["category"], out Guid parsed) ? parsed : null;
        string view = string.Equals(query["view"], Yearly, StringComparison.OrdinalIgnoreCase) ? Yearly : Monthly;
        int month = int.TryParse(query["month"], NumberStyles.None, CultureInfo.InvariantCulture, out int m) && m is >= 1 and <= 12
            ? m
            : currentMonth;

        return new BudgetDetailQuery(
            category,
            Search: query["q"].ToString().Trim(),
            Flow: query["flow"].ToString(),
            Attribution: query["attribution"].ToString(),
            // A checkbox submits its value when ticked and nothing when not
            Deleted: query["deleted"].Count > 0 && query["deleted"] != "false",
            view,
            month);
    }

    /// <summary>
    /// The address of the detail page in this state.
    /// </summary>
    public string Url(Guid budgetId)
    {
        QueryBuilder builder = [];

        if (Category is not null)
        {
            builder.Add("category", Category.Value.ToString());
        }

        if (Search.Length > 0)
        {
            builder.Add("q", Search);
        }

        if (Flow.Length > 0)
        {
            builder.Add("flow", Flow);
        }

        if (Attribution.Length > 0)
        {
            builder.Add("attribution", Attribution);
        }

        if (Deleted)
        {
            builder.Add("deleted", "true");
        }

        builder.Add("view", View);
        builder.Add("month", Month.ToString(CultureInfo.InvariantCulture));

        return $"/budgets/{budgetId}{builder.ToQueryString()}";
    }
}
