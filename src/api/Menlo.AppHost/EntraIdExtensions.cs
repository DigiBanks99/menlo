using Microsoft.Extensions.Configuration;

namespace Menlo.AppHost;

internal static class EntraIdExtensions
{
    public static EnvironmentCallbackContext AddEntraIdCredentials(this EnvironmentCallbackContext env, IConfiguration config)
    {
        env.EnvironmentVariables["AzureAd__Instance"] = config.GetValue("AzureAd:Instance", string.Empty);
        env.EnvironmentVariables["AzureAd__TenantId"] = config.GetValue("AzureAd:TenantId", string.Empty);
        env.EnvironmentVariables["AzureAd__Domain"] = config.GetValue("AzureAd:Domain", string.Empty);
        env.EnvironmentVariables["AzureAd__ClientId"] = config.GetValue("AzureAd:ClientId", string.Empty);
        env.EnvironmentVariables["AzureAd__ClientCredentials__0__ClientSecret"] = config.GetValue("AzureAd:ClientCredentials:0:ClientSecret", string.Empty);
        env.EnvironmentVariables["AzureAd__CookieDomain"] = config.GetValue("AzureAd:CookieDomain", string.Empty);
        return env;
    }
}
