using Menlo.Ui.Components;
using Menlo.Ui.Layouts;
using System.Security.Claims;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// What every full page needs besides its own model: where we are, who is signed in, and any toasts
/// carried over from the request that redirected here. Endpoints build it; pages pass it to AppPage.
/// </summary>
/// <param name="CurrentPath">The request path, used to mark the current navigation item.</param>
/// <param name="UserName">The signed-in person's display name, or null when nobody is signed in.</param>
/// <param name="Toasts">Messages to show as the page loads.</param>
public sealed record PageContext(string CurrentPath, string? UserName, IReadOnlyList<MnlToastMessage> Toasts)
{
    /// <summary>The app's primary navigation.</summary>
    public static IReadOnlyList<MnlNavItem> NavItems =>
    [
        new("Home", "/", MnlIconName.House),
        new("Budgets", "/budgets", MnlIconName.Wallet),
        new("Analytics", "/analytics", MnlIconName.TrendingUp),
        .. PrototypeNav()
    ];

    /// <summary>PROTOTYPE (#768): extra navigation items the payments prototype adds in Development.</summary>
    public static Func<IEnumerable<MnlNavItem>> PrototypeNav { get; set; } = () => [];

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

        return new PageContext(context.Request.Path.Value ?? "/", userName, Flash.Take(context));
    }
}
