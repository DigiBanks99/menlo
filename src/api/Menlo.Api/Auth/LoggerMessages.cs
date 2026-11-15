using System.Diagnostics.CodeAnalysis;

namespace Menlo.Api.Auth;

/// <summary>
/// Source-generated log messages for <see cref="UserContextMiddleware"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class LoggerMessages
{
    [LoggerMessage(LogLevel.Warning, "The signed-in session has no user id: the user could not be provisioned at sign-in.")]
    public static partial void MissingUserId(this ILogger logger);
}
