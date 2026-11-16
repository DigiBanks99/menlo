using Menlo.Ui.Components;

namespace Menlo.Api.Ui.Budgets;

/// <summary>
/// Presentation rules shared by the budget pages: which tone a status or a percentage gets.
/// They live here, in code, so pages only render what their model says.
/// </summary>
internal static class BudgetPresentation
{
    /// <summary>The names of the months, January first.</summary>
    public static IReadOnlyList<string> MonthNames { get; } =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    /// <summary>Active is good news, Closed is neutral, anything else (Draft) needs attention.</summary>
    public static MnlTone StatusTone(string status) => status switch
    {
        "Active" => MnlTone.Success,
        "Closed" => MnlTone.Neutral,
        _ => MnlTone.Warning
    };

    /// <summary>How alarming a "percent of budget spent" figure is.</summary>
    public static MnlTone SpendTone(decimal percentage) => percentage switch
    {
        >= 95m => MnlTone.Error,
        >= 80m => MnlTone.Warning,
        _ => MnlTone.Success
    };

    /// <summary>Income reads as good news; expenses are informational.</summary>
    public static MnlTone FlowTone(string budgetFlow) =>
        budgetFlow == "Income" ? MnlTone.Success : MnlTone.Info;

    /// <summary>"1 category" or "N categories".</summary>
    public static string CategoryCount(int count) => count == 1 ? "1 category" : $"{count} categories";

    /// <summary>The name of a month, 1 to 12.</summary>
    public static string MonthName(int month) =>
        month is >= 1 and <= 12 ? MonthNames[month - 1] : $"Month {month}";
}
