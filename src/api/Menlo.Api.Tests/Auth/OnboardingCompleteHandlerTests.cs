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

        OnboardingCompleteHandler sut = new(CreateUserContext(user), CreateOnboardingContext(onboardingState));
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUserWithoutCompletedOnboarding_WhenHandlingRequirement_ContextFails()
    {
        User user = User.Create(new ExternalUserId(TestAuthHandler.DefaultUserId), TestAuthHandler.DefaultEmail, TestAuthHandler.DefaultName).Value;
        OnboardingState onboardingState = OnboardingState.Create(user.Id);

        OnboardingCompleteHandler sut = new(CreateUserContext(user), CreateOnboardingContext(onboardingState));
        AuthorizationHandlerContext context = CreateAuthorizationContext(TestAuthHandler.DefaultUserId);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
        context.HasFailed.ShouldBeTrue();
    }

    private static IUserContext CreateUserContext(params User[] users)
    {
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.Users.Returns(DbSetMock.Create(users));
        return userContext;
    }

    private static IOnboardingContext CreateOnboardingContext(params OnboardingState[] onboardingStates)
    {
        IOnboardingContext onboardingContext = Substitute.For<IOnboardingContext>();
        onboardingContext.OnboardingStates.Returns(DbSetMock.Create(onboardingStates));
        return onboardingContext;
    }

    private static AuthorizationHandlerContext CreateAuthorizationContext(string externalId)
    {
        ClaimsPrincipal user = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, externalId)], "TestScheme"));
        return new AuthorizationHandlerContext([new OnboardingCompleteRequirement()], user, null);
    }
}
