using Microsoft.Extensions.Options;

namespace Menlo.Api.Auth.Options;

/// <summary>
/// Validates <see cref="MenloAuthOptions"/> on startup.
/// </summary>
public sealed class MenloAuthOptionsValidator : IValidateOptions<MenloAuthOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MenloAuthOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Instance))
        {
            failures.Add("Instance is required.");
        }

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            failures.Add("TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add("ClientId is required.");
        }

        bool hasClientSecret = !string.IsNullOrWhiteSpace(options.ClientSecret);
        bool hasCertificates = options.ClientCertificates?.Any() == true;

        if (!hasClientSecret && !hasCertificates)
        {
            failures.Add("Either ClientSecret or ClientCertificates is required.");
        }

        if (string.IsNullOrWhiteSpace(options.CookieDomain))
        {
            failures.Add("CookieDomain is required.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
