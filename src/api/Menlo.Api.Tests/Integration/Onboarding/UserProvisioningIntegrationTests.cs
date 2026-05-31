using Menlo.Api.Auth;
using Menlo.Api.Tests.Fixtures;
using Menlo.Application.Common;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Menlo.Api.Tests.Integration.Onboarding;

[Collection("Onboarding")]
public sealed class UserProvisioningIntegrationTests(OnboardingPersistenceFixture fixture)
{
    [Fact]
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
            .FirstOrDefaultAsync(candidate => candidate.ExternalId == normalizedExternalId, TestContext.Current.CancellationToken);
        OnboardingState? onboardingState = await db.OnboardingStates
            .AsNoTracking()
            .FirstOrDefaultAsync(state => state.UserId == user!.Id, TestContext.Current.CancellationToken);

        ItShouldHaveProvisionedUser(user, email, displayName);
        ItShouldHaveProvisionedOnboardingState(onboardingState, user!.Id);
    }

    [Fact]
    public async Task GivenExistingUser_WhenProvisioningCalled_NoDuplicate()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();
        MenloOidcEvents sut = new(fixture.Services);

        await sut.OnTokenValidated(CreateTokenValidatedContext(externalId, email, displayName));
        await sut.OnTokenValidated(CreateTokenValidatedContext(externalId, $"updated-{email}", $"Updated {displayName}"));

        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId normalizedExternalId = new(externalId);
        int userCount = await db.Users.CountAsync(user => user.ExternalId == normalizedExternalId, TestContext.Current.CancellationToken);
        User persistedUser = await db.Users
            .AsNoTracking()
            .SingleAsync(user => user.ExternalId == normalizedExternalId, TestContext.Current.CancellationToken);
        int onboardingStateCount = await db.OnboardingStates
            .CountAsync(state => state.UserId == persistedUser.Id, TestContext.Current.CancellationToken);

        userCount.ShouldBe(1);
        onboardingStateCount.ShouldBe(1);
        persistedUser.Email.ShouldBe(email);
        persistedUser.DisplayName.ShouldBe(displayName);
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

    private static void ItShouldHaveProvisionedOnboardingState(OnboardingState? onboardingState, Menlo.Lib.Common.ValueObjects.UserId expectedUserId)
    {
        onboardingState.ShouldNotBeNull();
        onboardingState.UserId.ShouldBe(expectedUserId);
        onboardingState.HasSelectedHousehold.ShouldBeFalse();
    }
}
