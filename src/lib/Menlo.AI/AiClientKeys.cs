namespace Menlo.AI;

/// <summary>
/// Service keys for the keyed model clients <c>AddMenloAi</c> registers.
/// </summary>
public static class AiClientKeys
{
    /// <summary>
    /// The <see cref="Microsoft.Extensions.AI.IChatClient"/> for the decision model, <c>Menlo:AI:DecisionModel</c>
    /// (falling back to <c>Menlo:AI:ChatModel</c>). Resolve it with <c>[FromKeyedServices(AiClientKeys.Decision)]</c>.
    /// </summary>
    public const string Decision = "Menlo.AI.Decision";
}
