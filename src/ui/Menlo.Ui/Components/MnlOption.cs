namespace Menlo.Ui.Components;

/// <summary>
/// One choice in a <see cref="MnlSelect"/>.
/// </summary>
/// <param name="Value">The value submitted with the form.</param>
/// <param name="Label">The text shown to the person.</param>
public sealed record MnlOption(string Value, string Label);
