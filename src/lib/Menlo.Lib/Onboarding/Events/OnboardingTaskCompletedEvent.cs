using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Onboarding;

namespace Menlo.Lib.Onboarding.Events;

public sealed record OnboardingTaskCompletedEvent(
    Guid UserId,
    OnboardingTaskType TaskType,
    Dictionary<string, object>? Metadata,
    DateTime Timestamp) : IDomainEvent;
