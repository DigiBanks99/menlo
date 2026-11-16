using Menlo.Ui.Components;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// The choices offered by the category and item forms.
/// </summary>
internal static class CategoryOptions
{
    /// <summary>The flows a category can have.</summary>
    public static IReadOnlyList<MnlOption> Flows { get; } =
    [
        new("Income", "Income"),
        new("Expense", "Expense"),
        new("Both", "Both")
    ];

    /// <summary>The flows an item can have.</summary>
    public static IReadOnlyList<MnlOption> ItemFlows { get; } =
    [
        new("Income", "Income"),
        new("Expense", "Expense")
    ];

    /// <summary>The parts of the household an amount can be attributed to.</summary>
    public static IReadOnlyList<MnlOption> Attributions { get; } =
    [
        new("Main", "Main"),
        new("Rental", "Rental"),
        new("ServiceProvider", "Service Provider")
    ];

    /// <summary>The display name of an attribution value.</summary>
    public static string AttributionLabel(string value) =>
        Attributions.FirstOrDefault(option => option.Value == value)?.Label ?? value;
}
