using Menlo.Lib.Onboarding;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Onboarding;

public interface IOnboardingContext
{
    DbSet<OnboardingState> OnboardingStates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
