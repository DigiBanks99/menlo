using Menlo.Ui.Components;
using Menlo.Ui.Layouts;
using System.Security.Claims;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// What every full page needs besides its own model: where we are, who is signed in, which navigation items
/// to show, and any toasts carried over from the request that redirected here. Endpoints build it; pages pass it to AppPage.
/// </summary>
/// <param name="CurrentPath">The request path, used to mark the current navigation item.</param>
/// <param name="UserName">The signed-in person's display name, or null when nobody is signed in.</param>
/// <param name="Toasts">Messages to show as the page loads.</param>
/// <param name="NavItems">The primary navigation, without the items of features that are switched off.</param>
public sealed record PageContext(
    string CurrentPath,
    string? UserName,
    IReadOnlyList<MnlToastMessage> Toasts,
    IReadOnlyList<MnlNavItem> NavItems)
{
    /// <summary>The configuration flag that switches the budget on.</summary>
    public const string BudgetFeatureFlag = "Features:Budget";

    private static readonly MnlNavItem Home = new("Home", "/", MnlIconName.House);
    private static readonly MnlNavItem Budget = new("Budgets", "/budgets", MnlIconName.Wallet);
    private static readonly MnlNavItem Analytics = new("Analytics", "/analytics", MnlIconName.TrendingUp);

    /// <summary>
    /// The app's primary navigation. The Budget item shows only while <see cref="BudgetFeatureFlag"/> is on.
    /// </summary>
    public static IReadOnlyList<MnlNavItem> NavItemsFor(IConfiguration configuration) =>
        configuration.GetValue<bool>(BudgetFeatureFlag) ? [Home, Budget, Analytics] : [Home, Analytics];

    /// <summary>
    /// Builds the context for a request. Reading the flash toasts also clears them, so call this once per page.
    /// </summary>
    public static PageContext From(HttpContext context)
    {
        string? userName = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirstValue(ClaimTypes.Name)
              ?? context.User.FindFirstValue("name")
              ?? context.User.FindFirstValue(ClaimTypes.Email)
            : null;

        IConfiguration configuration = context.RequestServices.GetRequiredService<IConfiguration>();

        return new PageContext(context.Request.Path.Value ?? "/", userName, Flash.Take(context), NavItemsFor(configuration));
    }
}
