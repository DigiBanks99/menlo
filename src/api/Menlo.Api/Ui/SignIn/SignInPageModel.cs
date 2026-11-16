namespace Menlo.Api.Ui.SignIn;

/// <summary>
/// The sign-in page's view model.
/// </summary>
/// <param name="LoginUrl">Where the sign-in button sends the browser to start the Microsoft sign-in.</param>
/// <param name="Failed">Whether the previous sign-in attempt failed.</param>
public sealed record SignInPageModel(string LoginUrl, bool Failed);
