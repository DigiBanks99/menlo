namespace Menlo.AI.Tests;

/// <summary>
/// Captures what an action throws, so a test can act first and leave the <c>ItShould...</c> checks to assert on the
/// outcome.
/// </summary>
internal static class Catch
{
    public static Exception? Exception(Action action)
    {
        try
        {
            action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
