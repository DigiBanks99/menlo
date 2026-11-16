namespace Menlo.Api.Ui.Errors;

/// <summary>
/// A page that couldn't be shown, and where to go instead.
/// </summary>
public sealed record ProblemPageModel(string Title, string Message, string BackUrl, string BackLabel);
