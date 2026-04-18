using CSharpFunctionalExtensions;
using Menlo.Application.Auth;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System.Security.Claims;

namespace Menlo.Application.Tests.Infrastructure;

public sealed class UserContextProviderUnitTests
{
    private static IHttpContextAccessor CreateAccessor(params Claim[] claims)
    {
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }

    private static IHttpContextAccessor CreateAccessorWithNullContext()
    {
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        return accessor;
    }

    private static IUserContext CreateUserContext(params User[] users)
    {
        IUserContext userContext = Substitute.For<IUserContext>();
        DbSet<User> dbSet = DbSetMock.Create(users);
        userContext.Users.Returns(dbSet);
        return userContext;
    }

    [Fact]
    public async Task GetUserContextAsync_WithNullHttpContext_ReturnsUnauthenticatedError()
    {
        UserContextProvider sut = new(CreateAccessorWithNullContext(), CreateUserContext());

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<UnauthenticatedError>();
    }

    [Fact]
    public async Task GetUserContextAsync_WithNoClaims_ReturnsUnauthenticatedError()
    {
        UserContextProvider sut = new(CreateAccessor(), CreateUserContext());

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<UnauthenticatedError>();
    }

    [Fact]
    public async Task GetUserContextAsync_WithSubClaim_ResolvesUser()
    {
        ExternalUserId externalId = new("sub-user-id");
        (User user, HouseholdId householdId) = CreateUserWithHousehold(externalId);
        UserContextProvider sut = new(CreateAccessor(new Claim("sub", externalId.Value)), CreateUserContext(user));

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.HouseholdId.ShouldBe(householdId);
    }

    [Fact]
    public async Task GetUserContextAsync_WithOidClaim_ResolvesUser()
    {
        ExternalUserId externalId = new("oid-user-id");
        (User user, HouseholdId householdId) = CreateUserWithHousehold(externalId);
        UserContextProvider sut = new(CreateAccessor(new Claim("oid", externalId.Value)), CreateUserContext(user));

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.HouseholdId.ShouldBe(householdId);
    }

    [Fact]
    public async Task GetUserContextAsync_WithNameIdentifierClaim_ResolvesUser()
    {
        ExternalUserId externalId = new("nameid-user-id");
        (User user, HouseholdId householdId) = CreateUserWithHousehold(externalId);
        UserContextProvider sut = new(CreateAccessor(new Claim(ClaimTypes.NameIdentifier, externalId.Value)), CreateUserContext(user));

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.HouseholdId.ShouldBe(householdId);
    }

    [Fact]
    public async Task GetUserContextAsync_WithAppidClaim_ResolvesUser()
    {
        ExternalUserId externalId = new("appid-user-id");
        (User user, HouseholdId householdId) = CreateUserWithHousehold(externalId);
        UserContextProvider sut = new(CreateAccessor(new Claim("appid", externalId.Value)), CreateUserContext(user));

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.HouseholdId.ShouldBe(householdId);
    }

    [Fact]
    public async Task GetUserContextAsync_WithAzpClaim_ResolvesUser()
    {
        ExternalUserId externalId = new("azp-user-id");
        (User user, HouseholdId householdId) = CreateUserWithHousehold(externalId);
        UserContextProvider sut = new(CreateAccessor(new Claim("azp", externalId.Value)), CreateUserContext(user));

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.HouseholdId.ShouldBe(householdId);
    }

    [Fact]
    public async Task GetUserContextAsync_SubTakesPriorityOverOid()
    {
        ExternalUserId subId = new("sub-wins");
        (User userForSub, HouseholdId householdId) = CreateUserWithHousehold(subId);
        UserContextProvider sut = new(
            CreateAccessor(new Claim("sub", subId.Value), new Claim("oid", "oid-loses")),
            CreateUserContext(userForSub));

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(userForSub.Id);
        result.Value.HouseholdId.ShouldBe(householdId);
    }

    [Fact]
    public async Task GetUserContextAsync_UserNotFound_ReturnsUserNotFoundError()
    {
        UserContextProvider sut = new(CreateAccessor(new Claim("sub", "unknown-user")), CreateUserContext());

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        UserNotFoundError error = result.Error.ShouldBeOfType<UserNotFoundError>();
        error.ExternalId.ShouldBe("unknown-user");
    }

    [Fact]
    public async Task GetUserContextAsync_UserWithoutHousehold_ReturnsUserNotAssignedToHouseholdError()
    {
        ExternalUserId externalId = new("no-household-user");
        User user = User.Create(externalId, "user@test.com", "Test User").Value;
        UserContextProvider sut = new(CreateAccessor(new Claim("sub", externalId.Value)), CreateUserContext(user));

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        UserNotAssignedToHouseholdError error = result.Error.ShouldBeOfType<UserNotAssignedToHouseholdError>();
        error.ExternalId.ShouldBe(externalId.Value);
    }

    private static (User User, HouseholdId HouseholdId) CreateUserWithHousehold(ExternalUserId externalId)
    {
        User user = User.Create(externalId, "user@test.com", "Test User").Value;
        HouseholdId householdId = HouseholdId.NewId();
        UserTestHelper.SetHouseholdId(user, householdId);
        return (user, householdId);
    }
}

[Collection("Persistence")]
public sealed class UserContextProviderIntegrationTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task GetUserContextAsync_WithRealUser_ReturnsCorrectUserContext()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IHouseholdContext householdContext = scope.ServiceProvider.GetRequiredService<IHouseholdContext>();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();

        Household household = Household.Create($"Integration Household {Guid.NewGuid():N}").Value;
        householdContext.Households.Add(household);
        await householdContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        ExternalUserId externalId = new($"integration-{Guid.NewGuid():N}");
        User user = User.Create(
            externalId,
            $"integration-{Guid.NewGuid():N}@test.com",
            "Integration Test User").Value;
        UserTestHelper.SetHouseholdId(user, household.Id);

        userContext.Users.Add(user);
        await userContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", externalId.Value)]));
        accessor.HttpContext.Returns(httpContext);

        UserContextProvider sut = new(accessor, userContext);

        Result<UserContext, AuthError> result = await sut.GetUserContextAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.HouseholdId.ShouldBe(household.Id);
    }
}
