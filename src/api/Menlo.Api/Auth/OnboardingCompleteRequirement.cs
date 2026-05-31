using Menlo.Application.Auth;
using Menlo.Application.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Api.Auth;

public sealed class OnboardingCompleteRequirement : IAuthorizationRequirement
{
}

public sealed class OnboardingCompleteHandler(IServiceProvider serviceProvider)
    : AuthorizationHandler<OnboardingCompleteRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OnboardingCompleteRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Fail();
            return;
        }

        IUserContext userContext = serviceProvider.GetRequiredService<IUserContext>();
        IOnboardingContext onboardingContext = serviceProvider.GetRequiredService<IOnboardingContext>();

        Menlo.Lib.Auth.Entities.User? user = await CurrentUserLookup.FindUserAsync(
            context.User,
            userContext.Users.AsNoTracking(),
            CancellationToken.None);

        if (user is null)
        {
            context.Fail();
            return;
        }

        var onboardingState = await onboardingContext.OnboardingStates
            .AsNoTracking()
            .FirstOrDefaultAsync(state => state.UserId == user.Id, CancellationToken.None);

        if (onboardingState?.HasSelectedHousehold == true)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
