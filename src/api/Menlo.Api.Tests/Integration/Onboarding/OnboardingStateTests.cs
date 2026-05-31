using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Onboarding;
using Menlo.Lib.Onboarding.Events;

namespace Menlo.Api.Tests.Integration.Onboarding;

public sealed class OnboardingStateTests
{
    [Fact]
    public void GivenNewOnboardingState_WhenQuerying_HasNoCompletedTasks()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());

        ItShouldHaveNoCompletedTasks(onboardingState);
        ItShouldNotHaveSelectedHousehold(onboardingState);
    }

    [Fact]
    public void GivenOnboardingState_WhenCompleteTaskCalled_TaskIsAdded()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());

        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        ItShouldContainSingleCompletedTask(onboardingState, OnboardingTaskType.SelectedHousehold);
        ItShouldBeFullyOnboarded(onboardingState);
    }

    [Fact]
    public void GivenOnboardingState_WhenCompleteTaskCalledTwice_TaskIsNotDuplicated()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());

        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);
        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        ItShouldContainSingleCompletedTask(onboardingState, OnboardingTaskType.SelectedHousehold);
    }

    [Fact]
    public void GivenOnboardingStateWithSelectedHouseholdTask_HasSelectedHousehold_ReturnsTrue()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());
        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        onboardingState.HasSelectedHousehold.ShouldBeTrue();
    }

    [Fact]
    public void CompleteTask_WithMetadata_PreservesMetadata()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());
        Dictionary<string, object> metadata = new() { { "householdId", "test-123" }, { "source", "mobile" } };

        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold, metadata);

        ItShouldContainTaskWithMetadata(onboardingState, OnboardingTaskType.SelectedHousehold, metadata);
    }

    [Fact]
    public void CompleteTask_TransitioningFromIncompleteToComplete_FiresOnboardingCompletedEvent()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());

        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        ItShouldHaveFiredOnboardingCompletedEvent(onboardingState);
        ItShouldContainOnboardingTaskCompletedEvent(onboardingState);
    }

    [Fact]
    public void CompleteTask_AlreadyCompleted_NoEventFired()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());
        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        // Clear events to isolate the second call
        onboardingState.ClearDomainEvents();

        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        ItShouldHaveNoDomainEvents(onboardingState);
    }

    [Fact]
    public void ClearDomainEvents_WithEvents_ClearsAll()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());
        onboardingState.CompleteTask(OnboardingTaskType.SelectedHousehold);

        ItShouldHaveDomainEvents(onboardingState);

        onboardingState.ClearDomainEvents();

        ItShouldHaveNoDomainEvents(onboardingState);
    }

    [Fact]
    public void AddDomainEvent_WithCustomEvent_AddsToCollection()
    {
        OnboardingState onboardingState = OnboardingState.Create(UserId.NewId());
        var customEvent = new OnboardingTaskCompletedEvent(
            onboardingState.UserId.Value,
            OnboardingTaskType.SelectedHousehold,
            null,
            DateTime.UtcNow);

        onboardingState.AddDomainEvent(customEvent);

        ItShouldContainDomainEvent(onboardingState, customEvent);
    }

    private static void ItShouldHaveNoCompletedTasks(OnboardingState onboardingState)
    {
        onboardingState.CompletedTasks.ShouldBeEmpty();
    }

    private static void ItShouldNotHaveSelectedHousehold(OnboardingState onboardingState)
    {
        onboardingState.HasSelectedHousehold.ShouldBeFalse();
    }

    private static void ItShouldContainSingleCompletedTask(OnboardingState onboardingState, OnboardingTaskType expectedTaskType)
    {
        onboardingState.CompletedTasks.Count.ShouldBe(1);
        onboardingState.CompletedTasks.Single().TaskType.ShouldBe(expectedTaskType);
    }

    private static void ItShouldBeFullyOnboarded(OnboardingState onboardingState)
    {
        onboardingState.IsFullyOnboarded.ShouldBeTrue();
    }

    private static void ItShouldContainTaskWithMetadata(
        OnboardingState onboardingState,
        OnboardingTaskType expectedTaskType,
        Dictionary<string, object> expectedMetadata)
    {
        onboardingState.CompletedTasks.Count.ShouldBe(1);
        OnboardingTask task = onboardingState.CompletedTasks.Single();
        task.TaskType.ShouldBe(expectedTaskType);
        task.Metadata.ShouldNotBeNull();
        task.Metadata.ShouldBe(expectedMetadata);
    }

    private static void ItShouldHaveFiredOnboardingCompletedEvent(OnboardingState onboardingState)
    {
        onboardingState.DomainEvents
            .OfType<OnboardingCompletedEvent>()
            .Count()
            .ShouldBe(1);
    }

    private static void ItShouldContainOnboardingTaskCompletedEvent(OnboardingState onboardingState)
    {
        onboardingState.DomainEvents
            .OfType<OnboardingTaskCompletedEvent>()
            .Count()
            .ShouldBe(1);
    }

    private static void ItShouldHaveDomainEvents(OnboardingState onboardingState)
    {
        onboardingState.DomainEvents.ShouldNotBeEmpty();
    }

    private static void ItShouldHaveNoDomainEvents(OnboardingState onboardingState)
    {
        onboardingState.DomainEvents.ShouldBeEmpty();
    }

    private static void ItShouldContainDomainEvent(OnboardingState onboardingState, IDomainEvent expectedEvent)
    {
        onboardingState.DomainEvents.ShouldContain(expectedEvent);
    }
}
