using Menlo.Api.Auth;
using Menlo.Api.Tests.Fixtures;
using Menlo.Application.Common;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Menlo.Api.Tests.Integration.Users;

[ClassDataSource<UserPersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Users")]
public sealed class UserProvisioningIntegrationTests(UserPersistenceFixture fixture)
{
    [Test]
    public async Task GivenNewOidcUser_WhenTokenValidated_UserIsProvisioned()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();
        MenloOidcEvents sut = new(fixture.Services);
        TokenValidatedContext context = CreateTokenValidatedContext(externalId, email, displayName);

        await sut.OnTokenValidated(context);

        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalizedExternalId = new(externalId);
        User? user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);

        ItShouldHaveProvisionedUser(user, email, displayName);
    }

    [Test]
    public async Task GivenExistingUser_WhenProvisioningCalled_NoDuplicate()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();
        MenloOidcEvents sut = new(fixture.Services);

        await sut.OnTokenValidated(CreateTokenValidatedContext(externalId, email, displayName));
        await sut.OnTokenValidated(CreateTokenValidatedContext(externalId, $"updated-{email}", $"Updated {displayName}"));

        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalizedExternalId = new(externalId);
        int userCount = await db.Users.CountAsync(user => user.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);
        User persistedUser = await db.Users
            .AsNoTracking()
            .SingleAsync(user => user.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);

        userCount.ShouldBe(1);
        persistedUser.Email.ShouldBe(email);
        persistedUser.DisplayName.ShouldBe(displayName);
    }

    [Test]
    public async Task GivenNewOidcUser_WhenTokenValidated_TheSessionCarriesTheUsersId()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();
        MenloOidcEvents sut = new(fixture.Services);
        TokenValidatedContext context = CreateTokenValidatedContext(externalId, email, displayName);

        await sut.OnTokenValidated(context);

        User stored = await FindUserAsync(externalId);
        UserSessionClaims.FindUserId(context.Principal!).ShouldBe(stored.Id);
    }

    [Test]
    public async Task GivenASessionWithoutAUserId_WhenValidated_TheUserIsProvisionedAndTheSessionRenewed()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(externalId, email, displayName);

        await MenloOidcEvents.OnValidatePrincipal(context);

        User stored = await FindUserAsync(externalId);
        UserSessionClaims.FindUserId(context.Principal!).ShouldBe(stored.Id);
        context.ShouldRenew.ShouldBeTrue();
    }

    [Test]
    public async Task GivenASessionThatAlreadyHasAUserId_WhenValidated_NothingIsLookedUpOrChanged()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(
            externalId, email, displayName, new Claim(UserSessionClaims.UserIdClaimType, UserId.NewId().Value.ToString()));
        context.HttpContext.RequestServices = new ServiceCollection().BuildServiceProvider();

        await MenloOidcEvents.OnValidatePrincipal(context);

        context.ShouldRenew.ShouldBeFalse();
    }

    [Test]
    public async Task GivenASessionWithoutAnEmail_WhenValidated_NoUserIsCreatedAndNoClaimAdded()
    {
        (string externalId, _, string displayName) = CreateProvisioningInput();
        CookieValidatePrincipalContext context = CreateValidatePrincipalContext(externalId, "", displayName);

        await MenloOidcEvents.OnValidatePrincipal(context);

        UserSessionClaims.HasUserId(context.Principal!).ShouldBeFalse();
        context.ShouldRenew.ShouldBeFalse();
    }

    private async Task<User> FindUserAsync(string externalId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalized = new(externalId);
        return await db.Users.AsNoTracking()
            .SingleAsync(user => user.ExternalId == normalized, TestContext.Current!.Execution.CancellationToken);
    }

    private CookieValidatePrincipalContext CreateValidatePrincipalContext(
        string externalId, string email, string displayName, params Claim[] extraClaims)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, externalId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, displayName),
            .. extraClaims
        ], "Cookies"));

        DefaultHttpContext httpContext = new() { RequestServices = fixture.Services };
        AuthenticationScheme scheme = new("Cookies", "Cookies", typeof(TestAuthHandler));
        AuthenticationTicket ticket = new(principal, "Cookies");
        return new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(), ticket);
    }

    private static TokenValidatedContext CreateTokenValidatedContext(string externalId, string email, string displayName)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, externalId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, displayName)
        ], "oidc"));

        DefaultHttpContext httpContext = new();
        AuthenticationScheme scheme = new("oidc", "oidc", typeof(TestAuthHandler));
        return new TokenValidatedContext(httpContext, scheme, new OpenIdConnectOptions(), principal, new AuthenticationProperties());
    }

    private static (string externalId, string email, string displayName) CreateProvisioningInput()
    {
        string uniqueSuffix = Guid.NewGuid().ToString("N");
        return ($"external-{uniqueSuffix}", $"user-{uniqueSuffix}@menlo.app", $"User {uniqueSuffix}");
    }

    private static void ItShouldHaveProvisionedUser(User? user, string expectedEmail, string expectedDisplayName)
    {
        user.ShouldNotBeNull();
        user.Email.ShouldBe(expectedEmail);
        user.DisplayName.ShouldBe(expectedDisplayName);
    }
}
