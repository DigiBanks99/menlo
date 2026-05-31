using Menlo.Api.Tests.Fixtures;
using Menlo.Application.Auth;
using Menlo.Application.Onboarding;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Models;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net.Http.Json;

namespace Menlo.Api.Tests.Auth;

public sealed class UserEndpointTests : TestFixture, IDisposable
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly User _user;

    public UserEndpointTests()
    {
        _user = User.Create(new ExternalUserId(TestAuthHandler.DefaultUserId), TestAuthHandler.DefaultEmail, TestAuthHandler.DefaultName).Value;
        OnboardingState onboardingState = OnboardingState.Create(_user.Id);

        _factory = new UserEndpointTestWebApplicationFactory(_user, onboardingState)
        {
            UserRoles = ["Menlo.User", "Menlo.Admin"]
        };
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GivenAnAuthenticatedUser_WhenRequestingUserProfile_AndUserHasRoles()
    {
        HttpResponseMessage response = await _client.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? userProfile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveTheUserProfile(userProfile, _user);
        ItShouldIncludeUserRoles(userProfile);
        ItShouldShowIncompleteOnboarding(userProfile);
    }

    [Fact]
    public async Task GivenAnUnauthenticatedUser_WhenRequestingUserProfile()
    {
        using TestWebApplicationFactory unauthenticatedFactory = new()
        {
            SimulateUnauthenticated = true
        };
        using HttpClient unauthenticatedClient = unauthenticatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        HttpResponseMessage response = await unauthenticatedClient.GetAsync("/auth/user", TestContext.Current.CancellationToken);

        ItShouldHaveBeenUnauthorised(response);
        ItShouldNotHaveRedirected(response);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenRequestingUserProfile_AndUserHasNoRoles()
    {
        OnboardingState onboardingState = OnboardingState.Create(_user.Id);
        using TestWebApplicationFactory noRolesFactory = new UserEndpointTestWebApplicationFactory(_user, onboardingState)
        {
            UserRoles = []
        };
        using HttpClient noRolesClient = noRolesFactory.CreateClient();

        HttpResponseMessage response = await noRolesClient.GetAsync("/auth/user", TestContext.Current.CancellationToken);
        UserProfile? userProfile = await response.Content.ReadFromJsonAsync<UserProfile>(TestContext.Current.CancellationToken);

        ItShouldHaveSucceeded(response);
        ItShouldHaveTheUserProfile(userProfile, _user);
        ItShouldHaveEmptyRolesList(userProfile);
    }

    private static void ItShouldHaveTheUserProfile(UserProfile? userProfile, User expectedUser)
    {
        userProfile.ShouldNotBeNull();
        userProfile.Id.ShouldBe(expectedUser.Id.Value.ToString());
        userProfile.Email.ShouldBe(TestAuthHandler.DefaultEmail);
        userProfile.DisplayName.ShouldBe(TestAuthHandler.DefaultName);
    }

    private static void ItShouldIncludeUserRoles(UserProfile? userProfile)
    {
        userProfile.ShouldNotBeNull();
        userProfile.Roles.ShouldContain("Menlo.User");
        userProfile.Roles.ShouldContain("Menlo.Admin");
    }

    private static void ItShouldHaveEmptyRolesList(UserProfile? userProfile)
    {
        userProfile.ShouldNotBeNull();
        userProfile.Roles.ShouldBeEmpty();
    }

    private static void ItShouldShowIncompleteOnboarding(UserProfile? userProfile)
    {
        userProfile.ShouldNotBeNull();
        userProfile.Onboarding.ShouldNotBeNull();
        userProfile.Onboarding.IsComplete.ShouldBeFalse();
        userProfile.Onboarding.PendingTasks.ShouldContain("SelectHousehold");
    }

    private static void ItShouldNotHaveRedirected(HttpResponseMessage response)
    {
        response.Headers.Location.ShouldBeNull();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private sealed class UserEndpointTestWebApplicationFactory(User user, OnboardingState onboardingState) : TestWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                ReplaceUserContext(services, user);
                ReplaceOnboardingContext(services, onboardingState);
            });
        }

        private static void ReplaceUserContext(IServiceCollection services, User currentUser)
        {
            ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IUserContext));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            IUserContext userContext = Substitute.For<IUserContext>();
            userContext.Users.Returns(DbSetMock.Create(currentUser));
            services.AddScoped(_ => userContext);
        }

        private static void ReplaceOnboardingContext(IServiceCollection services, OnboardingState currentOnboardingState)
        {
            ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IOnboardingContext));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            IOnboardingContext onboardingContext = Substitute.For<IOnboardingContext>();
            onboardingContext.OnboardingStates.Returns(DbSetMock.Create(currentOnboardingState));
            services.AddScoped(_ => onboardingContext);
        }
    }
}
