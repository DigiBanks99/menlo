using Menlo.Ui.Components;

namespace Menlo.Api.Ui.Home;

/// <summary>
/// The home page's view model.
/// </summary>
public sealed record HomePageModel(
    int CurrentYear,
    IReadOnlyList<HomeFeature> Features,
    IReadOnlyList<HomeStat> OverviewStats)
{
    /// <summary>
    /// The home page content. The overview figures are placeholders until the dashboard is fed by real data.
    /// </summary>
    public static HomePageModel Create(int currentYear) => new(
        currentYear,
        [
            new HomeFeature(
                "Smart budgeting",
                MnlTone.Success,
                "Plan together",
                "Track household spending with calm, readable summaries that keep the family's priorities in view."),
            new HomeFeature(
                "Handwritten lists",
                MnlTone.Info,
                "Bridge paper and digital",
                "Capture notes and lists quickly, then bring them back into the shared home-management flow."),
            new HomeFeature(
                "Privacy first",
                MnlTone.Neutral,
                "Keep data local",
                "Run Menlo on your own home server so sensitive household context stays close to the family.")
        ],
        [
            new HomeStat("Total budget", "R 12 000", "+4.8% vs last month", MnlTrendDirection.Up, MnlTone.Success),
            new HomeStat("Spent this month", "R 7 650", "Healthy pace", MnlTrendDirection.Neutral, MnlTone.Neutral),
            new HomeStat("Remaining", "36%", "R 4 350 left", MnlTrendDirection.Up, MnlTone.Success)
        ]);
}

/// <summary>A feature highlight card.</summary>
public sealed record HomeFeature(string Badge, MnlTone BadgeTone, string Title, string Description);

/// <summary>A figure in the overview card.</summary>
public sealed record HomeStat(string Label, string Value, string Trend, MnlTrendDirection Direction, MnlTone Tone);

/// <summary>
/// The current-budget widget's view model. Exactly one of <see cref="Budget"/> and <see cref="Error"/> is set.
/// </summary>
public sealed record BudgetWidgetModel(int Year, BudgetWidgetBudget? Budget, string? Error);

/// <summary>The budget shown in the widget.</summary>
public sealed record BudgetWidgetBudget(Guid Id, string Status, MnlTone StatusTone, string PlannedThisMonth);
