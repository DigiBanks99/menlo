using Microsoft.Extensions.Options;
using Microsoft.Identity.Abstractions;

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

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            failures.Add("ClientSecret is required.");
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
