namespace Menlo.AppHost;

internal static class EntraIdExtensions
{
    /// <summary>
    /// Declares Entra ID credentials as Aspire secret parameters and wires them into the
    /// project's environment as <c>AzureAd__*</c> variables.
    ///
    /// Locally, store values with:
    ///   aspire secret set Parameters:azure-ad-instance  "https://login.microsoftonline.com/"
    ///   aspire secret set Parameters:azure-ad-tenant-id "&lt;tenant-id&gt;"
    ///   aspire secret set Parameters:azure-ad-domain    "&lt;domain&gt;"
    ///   aspire secret set Parameters:azure-ad-client-id "&lt;client-id&gt;"
    ///   aspire secret set Parameters:azure-ad-client-secret "&lt;client-secret&gt;"
    ///
    /// In production the API container receives <c>AzureAd__*</c> environment variables
    /// directly (see cd-backend.yml), so the AppHost / these parameters are not involved.
    /// </summary>
    public static IResourceBuilder<ProjectResource> WithEntraIdCredentials(
        this IResourceBuilder<ProjectResource> projectBuilder,
        IDistributedApplicationBuilder appBuilder)
    {
        IResourceBuilder<ParameterResource> instance =
            appBuilder.AddParameter("azure-ad-instance");

        IResourceBuilder<ParameterResource> tenantId =
            appBuilder.AddParameter("azure-ad-tenant-id", secret: true);

        IResourceBuilder<ParameterResource> domain =
            appBuilder.AddParameter("azure-ad-domain");

        IResourceBuilder<ParameterResource> clientId =
            appBuilder.AddParameter("azure-ad-client-id", secret: true);

        IResourceBuilder<ParameterResource> clientSecret =
            appBuilder.AddParameter("azure-ad-client-secret", secret: true);

        return projectBuilder
            .WithEnvironment("AzureAd__Instance", instance)
            .WithEnvironment("AzureAd__TenantId", tenantId)
            .WithEnvironment("AzureAd__Domain", domain)
            .WithEnvironment("AzureAd__ClientId", clientId)
            .WithEnvironment("AzureAd__ClientCredentials__0__ClientSecret", clientSecret);
    }
}
