namespace Menlo.Lib.Auth.Models;

public sealed record OnboardingInfo(
    bool IsComplete,
    IReadOnlyList<string> PendingTasks);
