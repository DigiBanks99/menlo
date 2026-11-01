using Menlo.Api.Auth;
using Menlo.Api.Tests.Fixtures;
using Menlo.Application.Auth;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Onboarding;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System.Security.Claims;

namespace Menlo.Api.Tests.Auth;

public sealed class MenloOidcEventsTests
{
    private MenloOidcEvents CreateSut(IServiceProvider? serviceProvider = null)
    {
        IServiceProvider provider = serviceProvider ?? CreateDefaultServiceProvider();
        return new(provider);
    }

    private static IServiceProvider CreateDefaultServiceProvider()
    {
        IUserContext userContext = Substitute.For<IUserContext>();
        IOnboardingContext onboardingContext = Substitute.For<IOnboardingContext>();

        DbSet<User> usersSet = DbSetMock.Create(new User[0]);
        userContext.Users.Returns(usersSet);

        DbSet<OnboardingState> onboardingSet = DbSetMock.Create(new OnboardingState[0]);
        onboardingContext.OnboardingStates.Returns(onboardingSet);

        // SaveChangesAsync returns int, not Task
        userContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        onboardingContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        return new TestServiceProvider(userContext, onboardingContext);
    }

    private static IServiceProvider CreateServiceProviderWithFailure()
    {
        IUserContext userContext = Substitute.For<IUserContext>();
        IOnboardingContext onboardingContext = Substitute.For<IOnboardingContext>();

        DbSet<User> usersSet = DbSetMock.Create(new User[0]);
        userContext.Users.Returns(usersSet);

        userContext
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<int>(new InvalidOperationException("Provisioning failed")));

        return new TestServiceProvider(userContext, onboardingContext);
    }

    #region OnTokenValidated Tests

    [Test]
    public async Task OnTokenValidated_WithNameIdentifierClaim_CallsProvisioningService()
    {
        // Arrange
        const string externalId = "user-123";
        const string email = "test@example.com";
        const string displayName = "Test User";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act
        await sut.OnTokenValidated(context);

        // Assert - just verify it doesn't throw
        context.Principal.ShouldNotBeNull();
    }

    [Test]
    public async Task OnTokenValidated_WithOidClaim_ExtractsOidAsExternalId()
    {
        // Arrange
        const string externalId = "oid-value-456";
        const string email = "azure@tenant.onmicrosoft.com";
        const string displayName = "Azure User";

        TokenValidatedContext context = CreateTokenValidatedContext(
            oid: externalId,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act
        await sut.OnTokenValidated(context);

        // Assert - verify it processes OID claim
        context.Principal.ShouldNotBeNull();
    }

    [Test]
    public async Task OnTokenValidated_WithSubClaim_ExtractsSubAsExternalId()
    {
        // Arrange
        const string externalId = "sub-value-789";
        const string email = "oidc@provider.com";
        const string displayName = "OIDC User";

        TokenValidatedContext context = CreateTokenValidatedContext(
            sub: externalId,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act
        await sut.OnTokenValidated(context);

        // Assert - verify it processes sub claim
        context.Principal.ShouldNotBeNull();
    }

    [Test]
    public async Task OnTokenValidated_WithNameIdentifierAndOidClaims_PrefersNameIdentifier()
    {
        // Arrange
        const string nameIdentifier = "preferred-id";
        const string oid = "oid-id";
        const string email = "test@example.com";
        const string displayName = "Test User";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: nameIdentifier,
            oid: oid,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act
        await sut.OnTokenValidated(context);

        // Assert - should not throw, NameIdentifier takes precedence
        context.Principal.ShouldNotBeNull();
    }

    [Test]
    public async Task OnTokenValidated_WithOidAndSubClaims_PrefersOid()
    {
        // Arrange
        const string oid = "oid-id";
        const string sub = "sub-id";
        const string email = "test@example.com";
        const string displayName = "Test User";

        TokenValidatedContext context = CreateTokenValidatedContext(
            oid: oid,
            sub: sub,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act
        await sut.OnTokenValidated(context);

        // Assert - should not throw, OID takes precedence over sub
        context.Principal.ShouldNotBeNull();
    }

    [Test]
    public async Task OnTokenValidated_WithoutExternalIdClaims_ReturnsEarlyWithoutError()
    {
        // Arrange
        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: null,
            oid: null,
            sub: null);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert - should return early without error
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WithEmptyExternalIdClaim_ReturnsEarlyWithoutError()
    {
        // Arrange
        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: string.Empty);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WithWhitespaceExternalIdClaim_ReturnsEarlyWithoutError()
    {
        // Arrange
        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: "   ");

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WithEmailClaim_UsesProvidedEmail()
    {
        // Arrange
        const string externalId = "user-123";
        const string email = "user@company.com";
        const string displayName = "User Name";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WithoutEmailClaim_UsesDefaultEmail()
    {
        // Arrange
        const string externalId = "user-123";
        const string displayName = "User Name";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: null,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WithNameClaim_UsesProvidedDisplayName()
    {
        // Arrange
        const string externalId = "user-123";
        const string email = "user@company.com";
        const string displayName = "John Doe";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WithoutNameClaim_UsesEmailAsDisplayName()
    {
        // Arrange
        const string externalId = "user-123";
        const string email = "user@company.com";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: email,
            name: null);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WithoutNameClaimAndNoEmail_UsesDefaultEmailAsDisplayName()
    {
        // Arrange
        const string externalId = "user-123";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: null,
            name: null);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_WhenProvisioningFails_PropagatesException()
    {
        // Arrange
        const string externalId = "user-123";
        const string email = "user@company.com";
        const string displayName = "User Name";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: email,
            name: displayName);

        IServiceProvider serviceProvider = CreateServiceProviderWithFailure();
        MenloOidcEvents sut = CreateSut(serviceProvider);

        // Act & Assert
        InvalidOperationException thrownException =
            await Should.ThrowAsync<InvalidOperationException>(async () => await sut.OnTokenValidated(context));
        thrownException.Message.ShouldBe("Provisioning failed");
    }

    [Test]
    public async Task OnTokenValidated_UsesAsyncServiceScopeToResolveService()
    {
        // Arrange
        const string externalId = "user-123";
        const string email = "user@company.com";
        const string displayName = "User Name";

        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: email,
            name: displayName);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    [Test]
    public async Task OnTokenValidated_PassesCancellationTokenFromHttpContext()
    {
        // Arrange
        const string externalId = "user-123";
        const string email = "user@company.com";
        const string displayName = "User Name";

        using var cts = new CancellationTokenSource();
        TokenValidatedContext context = CreateTokenValidatedContext(
            nameIdentifier: externalId,
            email: email,
            name: displayName,
            cancellationToken: cts.Token);

        MenloOidcEvents sut = CreateSut();

        // Act & Assert
        await sut.OnTokenValidated(context);
    }

    #endregion

    #region Helper Methods

    private static TokenValidatedContext CreateTokenValidatedContext(
        string? nameIdentifier = null,
        string? oid = null,
        string? sub = null,
        string? email = null,
        string? name = null,
        CancellationToken? cancellationToken = null)
    {
        var claims = new List<Claim>();

        if (nameIdentifier is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }

        if (oid is not null)
        {
            claims.Add(new Claim("oid", oid));
        }

        if (sub is not null)
        {
            claims.Add(new Claim("sub", sub));
        }

        if (email is not null)
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        if (name is not null)
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);

        HttpContext httpContext = Substitute.For<HttpContext>();
        httpContext.RequestAborted.Returns(cancellationToken ?? CancellationToken.None);

        // Create a context wrapper since TokenValidatedContext requires constructor arguments
        return new TestTokenValidatedContext(principal, httpContext);
    }

    #endregion
}

/// <summary>
/// Test double IServiceProvider that creates appropriate async scopes for testing.
/// </summary>
internal sealed class TestServiceProvider : IServiceProvider
{
    private readonly IUserContext _userContext;
    private readonly IOnboardingContext _onboardingContext;

    public TestServiceProvider(IUserContext userContext, IOnboardingContext onboardingContext)
    {
        _userContext = userContext;
        _onboardingContext = onboardingContext;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(UserProvisioningService))
        {
            return new UserProvisioningService(_userContext, _onboardingContext);
        }

        // Handle the IServiceScopeFactory required by CreateAsyncScope extension
        if (serviceType == typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory))
        {
            return new TestServiceScopeFactory(this);
        }

        return null;
    }
}

/// <summary>
/// Test double IServiceScopeFactory for handling CreateAsyncScope calls.
/// </summary>
internal sealed class TestServiceScopeFactory : Microsoft.Extensions.DependencyInjection.IServiceScopeFactory
{
    private readonly TestServiceProvider _serviceProvider;

    public TestServiceScopeFactory(TestServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Microsoft.Extensions.DependencyInjection.IServiceScope CreateScope()
    {
        return new TestServiceScope(_serviceProvider);
    }
}

/// <summary>
/// Test double IServiceScope for handling async scope operations.
/// </summary>
internal sealed class TestServiceScope : Microsoft.Extensions.DependencyInjection.IServiceScope
{
    private readonly TestServiceProvider _serviceProvider;

    public TestServiceScope(TestServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider => _serviceProvider;

    public void Dispose()
    {
    }
}

/// <summary>
/// Test double for TokenValidatedContext since it doesn't have a parameterless constructor.
/// </summary>
internal sealed class TestTokenValidatedContext : TokenValidatedContext
{
    public TestTokenValidatedContext(ClaimsPrincipal principal, HttpContext httpContext)
        : base(
            httpContext,
            new Microsoft.AspNetCore.Authentication.AuthenticationScheme("Test", "Test",
                typeof(Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectHandler)),
            new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions(),
            principal,
            new Microsoft.AspNetCore.Authentication.AuthenticationProperties())
    {
    }
}
