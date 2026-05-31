using Menlo.Api.Auth;
using Menlo.Api.Tests.Fixtures;
using Menlo.Application.Auth;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using System.Security.Claims;

namespace Menlo.Api.Tests.Auth;

public sealed class OnboardingCompleteHandlerTests
{
    [Fact]
    public async Task GivenUserWithCompletedOnboarding_WhenHandlingRequirement_ContextSucceeds()
    {
        User user = User.Create(new ExternalUserId(TestAuthHandler.DefaultUserId), TestAuthHandler.DefaultEmail, TestAuthHandler.DefaultName).Value;
        OnboardingState onboardingState = OnboardingState.Create(user.Id);
        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        OnboardingCompleteHandler sut = new(CreateServices(CreateUserContext(user), CreateOnboardingContext(onboardingState)));
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUserWithoutCompletedOnboarding_WhenHandlingRequirement_ContextFails()
    {
        User user = User.Create(new ExternalUserId(TestAuthHandler.DefaultUserId), TestAuthHandler.DefaultEmail, TestAuthHandler.DefaultName).Value;
        OnboardingState onboardingState = OnboardingState.Create(user.Id);

        OnboardingCompleteHandler sut = new(CreateServices(CreateUserContext(user), CreateOnboardingContext(onboardingState)));
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        context.HasFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUnauthenticatedUser_WhenHandlingRequirement_ContextFails()
    {
        OnboardingCompleteHandler sut = new(CreateServices(CreateUserContext(), CreateOnboardingContext()));
        AuthorizationHandlerContext context = CreateUnauthenticatedAuthorizationContext();

        await sut.HandleAsync(context);

        AssertContextFailedWithoutSucceeding(context);
    }

    [Fact]
    public async Task GivenCurrentUserLookupReturnsNull_WhenHandlingRequirement_ContextFails()
    {
        User user = User.Create(new ExternalUserId(TestAuthHandler.DefaultUserId), TestAuthHandler.DefaultEmail, TestAuthHandler.DefaultName).Value;
        OnboardingState onboardingState = OnboardingState.Create(user.Id);

        // Create empty user context so lookup returns null
        OnboardingCompleteHandler sut = new(CreateServices(CreateUserContext(), CreateOnboardingContext(onboardingState)));
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        await sut.HandleAsync(context);

        AssertContextFailedWithoutSucceeding(context);
    }

    [Fact]
    public async Task GivenOnboardingStateIsNull_WhenHandlingRequirement_ContextFails()
    {
        User user = User.Create(new ExternalUserId(TestAuthHandler.DefaultUserId), TestAuthHandler.DefaultEmail, TestAuthHandler.DefaultName).Value;

        // Create empty onboarding context so state is null
        OnboardingCompleteHandler sut = new(CreateServices(CreateUserContext(user), CreateOnboardingContext()));
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        await sut.HandleAsync(context);

        AssertContextFailedWithoutSucceeding(context);
    }

    [Fact]
    public async Task GivenServiceProviderFailsToResolveUserContext_WhenHandlingRequirement_ThrowsInvalidOperationException()
    {
        IServiceProvider serviceProvider = CreateServiceProviderThatThrows();

        OnboardingCompleteHandler sut = new(serviceProvider);
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.HandleAsync(context));

        exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task GivenServiceProviderFailsToResolveOnboardingContext_WhenHandlingRequirement_ThrowsInvalidOperationException()
    {
        IUserContext userContext = CreateUserContext();
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IUserContext)).Returns(userContext);
        serviceProvider.GetService(typeof(IOnboardingContext)).Returns(x => throw new InvalidOperationException("Service not found"));

        OnboardingCompleteHandler sut = new(serviceProvider);
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => sut.HandleAsync(context));

        exception.ShouldNotBeNull();
    }

    private static IUserContext CreateUserContext(params User[] users)
    {
        IUserContext userContext = Substitute.For<IUserContext>();
        var userSet = DbSetMock.Create(users);
        userContext.Users.Returns(userSet);
        return userContext;
    }

    private static IOnboardingContext CreateOnboardingContext(params OnboardingState[] onboardingStates)
    {
        IOnboardingContext onboardingContext = Substitute.For<IOnboardingContext>();
        var onboardingSet = DbSetMock.Create(onboardingStates);
        onboardingContext.OnboardingStates.Returns(onboardingSet);
        return onboardingContext;
    }

    private static IServiceProvider CreateServices(IUserContext userContext, IOnboardingContext onboardingContext)
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IUserContext)).Returns(userContext);
        serviceProvider.GetService(typeof(IOnboardingContext)).Returns(onboardingContext);
        return serviceProvider;
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(string externalId)
    {
        ClaimsPrincipal user = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, externalId)], "TestScheme"));
        return new AuthorizationHandlerContext([new OnboardingCompleteRequirement()], user, null);
    }

    private static AuthorizationHandlerContext CreateUnauthenticatedAuthorizationContext()
    {
        ClaimsPrincipal user = new(new ClaimsIdentity([], "TestScheme"));
        return new AuthorizationHandlerContext([new OnboardingCompleteRequirement()], user, null);
    }

    private static IServiceProvider CreateServiceProviderThatThrows()
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IUserContext)).Returns(x => throw new InvalidOperationException("Service not found"));
        return serviceProvider;
    }

    private static void AssertContextFailedWithoutSucceeding(AuthorizationHandlerContext context)
    {
        context.HasSucceeded.ShouldBeFalse();
        context.HasFailed.ShouldBeTrue();
    }
}
