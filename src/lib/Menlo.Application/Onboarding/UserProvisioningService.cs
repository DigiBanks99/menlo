using Menlo.Application.Auth;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Onboarding;

public sealed class UserProvisioningService(
    IUserContext userContext,
    IOnboardingContext onboardingContext)
{
    public async Task ProvisionOrUpdateAsync(
        string externalId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        ExternalUserId normalizedExternalId = new(externalId);

        User? existingUser = await userContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.ExternalId == normalizedExternalId, cancellationToken);

        if (existingUser is not null)
        {
            return;
        }

        var userResult = User.Create(normalizedExternalId, email, displayName);
        if (userResult.IsFailure)
        {
            throw new InvalidOperationException(userResult.Error.Message);
        }

        User newUser = userResult.Value;

        userContext.Users.Add(newUser);
        await userContext.SaveChangesAsync(cancellationToken);

        OnboardingState onboardingState = OnboardingState.Create(newUser.Id);
        onboardingContext.OnboardingStates.Add(onboardingState);
        await onboardingContext.SaveChangesAsync(cancellationToken);
    }
}
