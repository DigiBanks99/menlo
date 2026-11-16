namespace Menlo.Ui.Components;

/// <summary>
/// A short message shown in the corner of the screen that disappears by itself.
/// </summary>
/// <param name="Message">The text.</param>
/// <param name="Tone">Success, Warning, Error or Info.</param>
public sealed record MnlToastMessage(string Message, MnlTone Tone = MnlTone.Info)
{
    /// <summary>
    /// The name of the browser event that shows a toast. Send it in an HX-Trigger response header with
    /// a detail of <c>{ "message": "...", "tone": "success" }</c>.
    /// </summary>
    public const string EventName = "mnl:toast";
}
