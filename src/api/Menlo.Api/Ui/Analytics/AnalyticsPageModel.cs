using Menlo.Ui.Components;

namespace Menlo.Api.Ui.Analytics;

/// <summary>
/// The analytics page's view model. The figures are sample data until the analytics API exists.
/// </summary>
public sealed record AnalyticsPageModel(
    string Period,
    string PeriodLabel,
    IReadOnlyList<MnlOption> Periods,
    IReadOnlyList<AnalyticsMetric> Metrics,
    IReadOnlyList<AnalyticsCategory> Categories,
    IReadOnlyList<AnalyticsInsight> Insights)
{
    private static readonly MnlOption[] PeriodOptions =
    [
        new("current", "Current Month"),
        new("last", "Last Month"),
        new("quarter", "This Quarter"),
        new("year", "This Year")
    ];

    /// <summary>
    /// The page for a period. An unknown period falls back to the current month.
    /// </summary>
    public static AnalyticsPageModel Create(string? period)
    {
        MnlOption selected = PeriodOptions.FirstOrDefault(option => option.Value == period) ?? PeriodOptions[0];

        return new AnalyticsPageModel(
            selected.Value,
            selected.Label,
            PeriodOptions,
            [
                new("1", "Total Budget", "R 15,500", "💰", MnlTone.Info, "+5.2% from last month", MnlTrendDirection.Up, MnlTone.Success),
                new("2", "Spent This Month", "R 11,350", "💸", MnlTone.Warning, "73% of budget used", MnlTrendDirection.Neutral, MnlTone.Neutral),
                new("3", "Remaining", "R 4,150", "🎯", MnlTone.Success, "12 days left", MnlTrendDirection.Up, MnlTone.Success),
                new("4", "Savings Rate", "23%", "📈", MnlTone.Success, "+2% vs target", MnlTrendDirection.Up, MnlTone.Success)
            ],
            [
                Category("1", "Groceries", 3200m, 85m, "🛒"),
                Category("2", "Utilities", 1800m, 75m, "💡"),
                Category("3", "Transport", 2100m, 70m, "🚗"),
                Category("4", "Entertainment", 850m, 42m, "🎬"),
                Category("5", "Healthcare", 1200m, 60m, "🏥"),
                Category("6", "Shopping", 2200m, 88m, "🛍️")
            ],
            [
                new("1", "Grocery Spending Alert", "You're spending 15% more on groceries than usual. Consider meal planning to reduce costs.", "⚠️", "Warning", MnlTone.Warning, "View Suggestions"),
                new("2", "Great Savings Progress", "You're ahead of your savings goal this month! Consider allocating extra funds to your emergency fund.", "🎉", "Success", MnlTone.Success, "Adjust Budget"),
                new("3", "Upcoming Bill Reminder", "Your electricity bill is typically due in 5 days. Budget shows you have sufficient funds allocated.", "💡", "Tip", MnlTone.Info, null)
            ]);
    }

    private static AnalyticsCategory Category(string id, string name, decimal amount, decimal percentage, string icon) =>
        new(id, name, $"R{amount:N0}", percentage, $"{percentage:0}%", icon, percentage switch
        {
            >= 95m => MnlTone.Error,
            >= 85m => MnlTone.Warning,
            _ => MnlTone.Success
        });
}

/// <summary>A headline figure.</summary>
public sealed record AnalyticsMetric(
    string Id,
    string Title,
    string Value,
    string Icon,
    MnlTone IconTone,
    string Change,
    MnlTrendDirection Direction,
    MnlTone ChangeTone);

/// <summary>A category's use of its allocation.</summary>
public sealed record AnalyticsCategory(
    string Id,
    string Name,
    string Amount,
    decimal Percentage,
    string PercentageText,
    string Icon,
    MnlTone Tone);

/// <summary>A piece of generated guidance.</summary>
public sealed record AnalyticsInsight(
    string Id,
    string Title,
    string Description,
    string Icon,
    string Label,
    MnlTone Tone,
    string? Action);
