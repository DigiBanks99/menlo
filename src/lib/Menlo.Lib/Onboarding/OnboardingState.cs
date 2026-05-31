using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Onboarding.Events;

namespace Menlo.Lib.Onboarding;

public sealed class OnboardingState : IAggregateRoot<OnboardingStateId>, IHasDomainEvents
{
    private static readonly OnboardingTaskType[] RequiredTaskTypes =
    [
        OnboardingTaskType.SelectedHousehold
    ];

    private readonly List<IDomainEvent> _domainEvents = [];

    private OnboardingState()
    {
        CompletedTasks = [];
    }

    private OnboardingState(
        OnboardingStateId id,
        UserId userId,
        IReadOnlyCollection<OnboardingTask> completedTasks,
        DateTime createdAt,
        DateTime updatedAt)
    {
        Id = id;
        UserId = userId;
        CompletedTasks = completedTasks;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public OnboardingStateId Id { get; private set; }

    public UserId UserId { get; private set; }

    public IReadOnlyCollection<OnboardingTask> CompletedTasks { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public bool HasSelectedHousehold => CompletedTasks.Any(task => task.TaskType == OnboardingTaskType.SelectedHousehold);

    public bool IsFullyOnboarded => RequiredTaskTypes.All(requiredTask =>
        CompletedTasks.Any(completedTask => completedTask.TaskType == requiredTask));

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static OnboardingState Create(UserId userId)
    {
        DateTime now = DateTime.UtcNow;

        return new OnboardingState(
            OnboardingStateId.NewId(),
            userId,
            [],
            now,
            now);
    }

    public void CompleteTask(OnboardingTaskType taskType, Dictionary<string, object>? metadata = null)
    {
        if (CompletedTasks.Any(task => task.TaskType == taskType))
        {
            return;
        }

        bool wasFullyOnboarded = IsFullyOnboarded;
        List<OnboardingTask> completedTasks = GetMutableCompletedTasks();
        DateTime completedAt = DateTime.UtcNow;

        completedTasks.Add(new OnboardingTask(taskType, completedAt, metadata));
        UpdatedAt = completedAt;

        AddDomainEvent(new OnboardingTaskCompletedEvent(UserId.Value, taskType, metadata, completedAt));

        if (!wasFullyOnboarded && IsFullyOnboarded)
        {
            AddDomainEvent(new OnboardingCompletedEvent(UserId.Value, completedAt));
        }
    }

    public void AddDomainEvent<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private List<OnboardingTask> GetMutableCompletedTasks()
    {
        if (CompletedTasks is List<OnboardingTask> completedTasks)
        {
            return completedTasks;
        }

        List<OnboardingTask> mutableCompletedTasks = [.. CompletedTasks];
        CompletedTasks = mutableCompletedTasks;
        return mutableCompletedTasks;
    }
}
