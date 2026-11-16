namespace Menlo.Ui.Components;

/// <summary>
/// One choice in a <see cref="MnlSegmented"/>.
/// </summary>
/// <param name="Value">Identifies the choice.</param>
/// <param name="Label">The text shown.</param>
/// <param name="Href">Where the choice links to.</param>
public sealed record MnlSegment(string Value, string Label, string Href);
