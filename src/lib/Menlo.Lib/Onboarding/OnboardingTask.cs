namespace Menlo.Lib.Onboarding;

public sealed record OnboardingTask
{
    private OnboardingTask()
    {
    }

    public OnboardingTaskType TaskType { get; init; }

    public DateTime CompletedAt { get; init; }

    public Dictionary<string, object>? Metadata { get; init; }

    public OnboardingTask(OnboardingTaskType taskType, DateTime completedAt, Dictionary<string, object>? metadata = null)
    {
        TaskType = taskType;
        CompletedAt = completedAt;
        Metadata = metadata;
    }
}
