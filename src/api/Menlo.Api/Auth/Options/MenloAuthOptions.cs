using Microsoft.Identity.Web;

namespace Menlo.Api.Auth.Options;

/// <summary>
/// Configuration options for Menlo authentication with Microsoft Entra ID.
/// </summary>
public sealed class MenloAuthOptions: MicrosoftIdentityOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AzureAd";

    /// <summary>
    /// Gets the cookie domain for cross-subdomain session sharing.
    /// </summary>
    public required string CookieDomain { get; init; }
}
