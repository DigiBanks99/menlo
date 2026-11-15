using CSharpFunctionalExtensions;
using Menlo.Api.Auth;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

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
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_testOptions.SimulateUnauthenticated)
        {
            return AuthenticateResult.NoResult();
        }

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, _testOptions.UserId),
            new(ClaimTypes.Email, _testOptions.Email),
            new(ClaimTypes.Name, _testOptions.DisplayName),
        ];

        claims.AddRange(_testOptions.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        ClaimsIdentity identity = new(claims, SchemeName);
        ClaimsPrincipal principal = new(identity);

        // Like sign-in does for the real session: provision the user and carry its id as a claim. When the claims can't
        // make a user, the claim stays off and the API answers 403.
        Result<User, AuthError> provisioned = await UserSessionClaims.ProvisionAsync(
            Context.RequestServices.GetRequiredService<IServiceScopeFactory>(),
            principal,
            Context.RequestAborted);
        if (provisioned.IsSuccess)
        {
            UserSessionClaims.AddUserId(principal, provisioned.Value.Id);
        }

        AuthenticationTicket ticket = new(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}


