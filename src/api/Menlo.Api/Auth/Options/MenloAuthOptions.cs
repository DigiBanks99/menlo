using Microsoft.Identity.Web;

namespace Menlo.Api.Auth.Options;

/// <summary>
/// Configuration options for Menlo authentication with Microsoft Entra ID.
/// </summary>
public sealed class MenloAuthOptions : MicrosoftIdentityOptions
{
    public const string SectionName = "AzureAd";
}
