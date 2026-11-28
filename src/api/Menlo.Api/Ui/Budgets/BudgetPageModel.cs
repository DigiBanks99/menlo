using Menlo.Lib.Budget;
using Menlo.Ui.Components;
using System.Globalization;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// What the budget page shows: which year, and which side rail section is open. Both are in the URL.
/// </summary>
/// <param name="Year">The calendar year.</param>
/// <param name="Panel">The key of the open side rail section, or null when the rail is closed.</param>
public sealed record BudgetQuery(int Year, string? Panel = null)
{
    /// <summary>The page's address.</summary>
    public const string Path = "/budgets";

    /// <summary>
    /// Reads the query string. A missing or impossible year falls back to the current year; an unknown section closes the rail.
    /// </summary>
    public static BudgetQuery From(HttpRequest request, int currentYear)
    {
        int year = int.TryParse(request.Query["year"], NumberStyles.None, CultureInfo.InvariantCulture, out int parsed)
                   && parsed is >= BudgetAggregate.MinYear and <= BudgetAggregate.MaxYear
            ? parsed
            : currentYear;

        string? panel = request.Query["panel"].ToString();
        return new BudgetQuery(year, BudgetRailSection.IsKnown(panel) ? panel : null);
    }

    /// <summary>The address of this state.</summary>
    public string Url() => Panel is null
        ? $"{Path}?year={Year.ToString(CultureInfo.InvariantCulture)}"
        : $"{Path}?year={Year.ToString(CultureInfo.InvariantCulture)}&panel={Uri.EscapeDataString(Panel)}";

    /// <summary>The address with a section opened, or closed when it is the one already open.</summary>
    public string Toggle(string panel) => (this with { Panel = Panel == panel ? null : panel }).Url();
}

/// <summary>
/// The budget page's view model.
/// </summary>
/// <param name="Year">The year shown.</param>
/// <param name="Title">The page heading, such as "2026 budget".</param>
/// <param name="Exists">Whether the year has a budget. When it hasn't, the page offers to create it.</param>
/// <param name="Years">The year picker's choices: the years with a budget, and the year shown.</param>
/// <param name="SelectedYear">The year picker's value.</param>
/// <param name="Months">The month strip: twelve months.</param>
/// <param name="YearTile">The month strip's last tile, for the whole year.</param>
/// <param name="Rail">The side rail's sections.</param>
/// <param name="OpenSection">The open section, or null.</param>
/// <param name="CloseUrl">The address that closes the open section.</param>
/// <param name="Offer">The offer to create the year's budget.</param>
public sealed record BudgetPageModel(
    int Year,
    string Title,
    bool Exists,
    IReadOnlyList<MnlOption> Years,
    string SelectedYear,
    IReadOnlyList<BudgetMonthTile> Months,
    BudgetMonthTile YearTile,
    IReadOnlyList<BudgetRailItem> Rail,
    BudgetRailItem? OpenSection,
    string CloseUrl,
    BudgetCreateOffer Offer)
{
    /// <summary>Whether to show the year picker: there is a budget to pick.</summary>
    public bool ShowYearPicker => Years.Count > 1 || Exists;

    /// <summary>"open" while a side rail section is open, otherwise "closed". The CSS lays the frame out by it.</summary>
    public string RailState => OpenSection is null ? "closed" : "open";

    /// <summary>
    /// Builds the page for a year.
    /// </summary>
    /// <param name="query">The year and open section.</param>
    /// <param name="exists">Whether the year has a budget.</param>
    /// <param name="years">The years that have a budget.</param>
    /// <param name="currentMonth">The current month, from <see cref="BudgetCalendar.CurrentMonth"/>.</param>
    public static BudgetPageModel Create(BudgetQuery query, bool exists, IReadOnlyList<int> years, DateOnly currentMonth)
    {
        string year = query.Year.ToString(CultureInfo.InvariantCulture);

        List<MnlOption> choices = [.. years.Union([query.Year]).Order().Select(y =>
        {
            string value = y.ToString(CultureInfo.InvariantCulture);
            return new MnlOption(value, y == query.Year && !exists ? $"{value} (no budget yet)" : value);
        })];

        BudgetMonthTile[] months =
        [
            .. Enumerable.Range(1, 12).Select(month => BudgetMonthTile.For(query.Year, month, currentMonth))
        ];

        BudgetRailItem[] rail =
        [
            .. BudgetRailSection.All.Select(section => new BudgetRailItem(
                section,
                query.Toggle(section.Key),
                section.Key == query.Panel))
        ];

        return new BudgetPageModel(
            query.Year,
            $"{year} budget",
            exists,
            choices,
            year,
            months,
            new BudgetMonthTile("Year", $"All of {year}", "year", null),
            rail,
            rail.FirstOrDefault(item => item.IsOpen),
            (query with { Panel = null }).Url(),
            new BudgetCreateOffer(
                BudgetQuery.Path,
                year,
                $"No budget for {year} yet",
                $"A budget plans what the household expects to earn and spend in {year}, month by month.",
                $"Create the {year} budget"));
    }
}

/// <summary>
/// A tile of the month strip.
/// </summary>
/// <param name="Name">The short name shown, such as "Jan".</param>
/// <param name="FullName">The full name, such as "January 2026".</param>
/// <param name="State">"frozen", "current" or "ahead", or "year" for the year tile. The CSS styles each.</param>
/// <param name="StateLabel">What the state means, for a frozen or the current month; null otherwise.</param>
public sealed record BudgetMonthTile(string Name, string FullName, string State, string? StateLabel)
{
    /// <summary>Whether this is the current month.</summary>
    public bool IsCurrent => State == "current";

    /// <summary>The value of aria-current: "date" for the current month.</summary>
    public string? AriaCurrent => IsCurrent ? "date" : null;

    /// <summary>The tile for a month of a year.</summary>
    public static BudgetMonthTile For(int year, int month, DateOnly currentMonth)
    {
        DateOnly first = new(year, month, 1);
        BudgetMonthState state = BudgetCalendar.StateOf(year, month, currentMonth);

        return new BudgetMonthTile(
            first.ToString("MMM", CultureInfo.InvariantCulture),
            first.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            state switch
            {
                BudgetMonthState.Frozen => "frozen",
                BudgetMonthState.Current => "current",
                _ => "ahead"
            },
            state switch
            {
                BudgetMonthState.Frozen => "Frozen",
                BudgetMonthState.Current => "This month",
                _ => null
            });
    }
}

/// <summary>
/// A section of the side rail.
/// </summary>
/// <param name="Key">Its key in the URL.</param>
/// <param name="Label">Its name.</param>
/// <param name="Icon">Its icon in the rail.</param>
/// <param name="Empty">What it says while it has nothing to show.</param>
public sealed record BudgetRailSection(string Key, string Label, MnlIconName Icon, string Empty)
{
    /// <summary>The sections, in the order the rail shows them.</summary>
    public static IReadOnlyList<BudgetRailSection> All { get; } =
    [
        new("proposals", "Proposals", MnlIconName.Lightbulb, "No proposals yet. A proposal tries out changes without touching the plan."),
        new("triggers", "Triggers", MnlIconName.Zap, "No triggers yet. A trigger is an expected event whose date may move."),
        new("checklist", "Set-up checklist", MnlIconName.ListChecks, "Nothing to check yet."),
        new("year", "Year tools", MnlIconName.Calendar, "No year tools yet."),
        new("who", "Who it is for", MnlIconName.Users, "Nothing to filter yet."),
        new("key", "How to read the grid", MnlIconName.Info, "Months before this month are frozen and shaded: their plan can no longer change. This month is marked.")
    ];

    /// <summary>Whether a key names a section.</summary>
    public static bool IsKnown(string? key) => All.Any(section => section.Key == key);
}

/// <summary>
/// A side rail section as the page shows it.
/// </summary>
/// <param name="Section">The section.</param>
/// <param name="ToggleUrl">The address that opens it, or closes it when it is open.</param>
/// <param name="IsOpen">Whether it is open.</param>
public sealed record BudgetRailItem(BudgetRailSection Section, string ToggleUrl, bool IsOpen)
{
    /// <summary>The value of the icon link's aria-expanded.</summary>
    public string Expanded => IsOpen ? "true" : "false";

    /// <summary>The value of aria-controls: the panel, while this section is the one open in it.</summary>
    public string? Controls => IsOpen ? BudgetRail.PanelId : null;
}

/// <summary>
/// The offer to create a year's budget.
/// </summary>
/// <param name="Action">Where the form posts.</param>
/// <param name="Year">The year, as the form sends it.</param>
/// <param name="Title">The empty state's heading.</param>
/// <param name="Message">The empty state's explanation.</param>
/// <param name="Label">The button's text.</param>
public sealed record BudgetCreateOffer(string Action, string Year, string Title, string Message, string Label);
