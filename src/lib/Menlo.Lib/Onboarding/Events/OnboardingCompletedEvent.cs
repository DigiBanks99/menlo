using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Onboarding.Events;

public sealed record OnboardingCompletedEvent(Guid UserId, DateTime Timestamp) : IDomainEvent;
