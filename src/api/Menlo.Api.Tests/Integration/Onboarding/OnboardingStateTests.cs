using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Onboarding;

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
}
