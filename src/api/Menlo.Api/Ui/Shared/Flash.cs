using Menlo.Ui.Components;
using System.Text.Json;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// Carries a toast across a redirect: the request that did the work stores it in a short-lived cookie,
/// and the next page shows it and clears it. For responses that don't redirect, send the toast with
/// <see cref="HxResponse"/> instead.
/// </summary>
internal static class Flash
{
    private const string CookieName = ".Menlo.Flash";

    /// <summary>
    /// Stores a toast for the next page.
    /// </summary>
    public static void Set(HttpContext context, string message, MnlTone tone = MnlTone.Success) =>
        context.Response.Cookies.Append(
            CookieName,
            JsonSerializer.Serialize(new FlashMessage(message, tone)),
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(1)
            });

    /// <summary>
    /// Returns the stored toast, if any, and clears it.
    /// </summary>
    public static IReadOnlyList<MnlToastMessage> Take(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out string? raw) || string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        context.Response.Cookies.Delete(CookieName);

        try
        {
            FlashMessage? message = JsonSerializer.Deserialize<FlashMessage>(raw);
            return message is null || string.IsNullOrWhiteSpace(message.Message)
                ? []
                : [new MnlToastMessage(message.Message, message.Tone)];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record FlashMessage(string Message, MnlTone Tone);
}
