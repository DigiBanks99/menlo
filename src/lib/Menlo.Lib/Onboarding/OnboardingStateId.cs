namespace Menlo.Lib.Onboarding;

public readonly record struct OnboardingStateId(Guid Value)
{
    public static OnboardingStateId NewId() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
