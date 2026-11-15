using System.Diagnostics.CodeAnalysis;

namespace Menlo.Api.Auth;

/// <summary>
/// Source-generated log messages for <see cref="UserContextMiddleware"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class LoggerMessages
{
    [LoggerMessage(LogLevel.Warning, "Cannot provision a user: the sign-in claims have no {Missing}.")]
    public static partial void MissingClaim(this ILogger logger, string missing);

    [LoggerMessage(LogLevel.Warning, "Cannot provision a user for the signed-in caller: {ErrorCode} {ErrorMessage}")]
    public static partial void ProvisioningFailed(this ILogger logger, string errorCode, string errorMessage);
}
