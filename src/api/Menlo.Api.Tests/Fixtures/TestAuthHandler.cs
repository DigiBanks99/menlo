using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Menlo.Api.Tests.Fixtures;

/// <summary>
/// Test authentication handler for integration tests.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    IOptionsMonitor<TestAuthHandlerOptions> testOptions,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// The authentication scheme name.
    /// </summary>
    public const string SchemeName = "TestScheme";

    /// <summary>
    /// Default test user ID.
    /// </summary>
    public const string DefaultUserId = "11111111-1111-1111-1111-111111111111";

    /// <summary>
    /// Default test user email.
    /// </summary>
    public const string DefaultEmail = "test@example.com";

    /// <summary>
    /// Default test user display name.
    /// </summary>
    public const string DefaultName = "Test User";

    private readonly TestAuthHandlerOptions _testOptions = testOptions.CurrentValue;

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_testOptions.SimulateUnauthenticated)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        List<Claim> claims =
        [
            new("oid", DefaultUserId),
            new(ClaimTypes.NameIdentifier, DefaultUserId),
            new(ClaimTypes.Email, DefaultEmail),
            new(ClaimTypes.Name, DefaultName),
        ];

        claims.AddRange(_testOptions.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        ClaimsIdentity identity = new(claims, SchemeName);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
