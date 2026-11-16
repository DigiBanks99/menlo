using System.Reflection;

namespace Menlo.Ui.Layouts;

/// <summary>
/// Addresses of the design system's static files, with a version suffix so browsers pick up new builds.
/// </summary>
public static class MnlAssets
{
    private const string BasePath = "/_content/Menlo.Ui/";

    /// <summary>
    /// A short token that changes with every build of the library or of the app that hosts it.
    /// The host's build matters too: its scoped-CSS bundle changes when only a page's styles do.
    /// </summary>
    public static string Version { get; } = string.Concat(
        typeof(MnlAssets).Assembly.ManifestModule.ModuleVersionId.ToString("N").AsSpan(0, 6),
        (Assembly.GetEntryAssembly() ?? typeof(MnlAssets).Assembly).ManifestModule.ModuleVersionId.ToString("N").AsSpan(0, 6));

    /// <summary>
    /// Returns the address of a file under the library's wwwroot, for example <c>css/tokens.css</c>.
    /// </summary>
    public static string Url(string path) => $"{BasePath}{path}?v={Version}";

    /// <summary>
    /// Returns the address of an app-level file (one served from the host's own root) with the version suffix.
    /// </summary>
    public static string HostUrl(string path) => $"/{path.TrimStart('/')}?v={Version}";
}
