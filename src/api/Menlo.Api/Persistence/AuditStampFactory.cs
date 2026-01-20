using System.Security.Claims;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Api.Persistence;

/// <summary>
/// Implementation of <see cref="IAuditStampFactory"/> that resolves the current user
/// from HttpContext and uses the system time provider.
/// </summary>
public sealed class AuditStampFactory : IAuditStampFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditStampFactory"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">The HTTP context accessor for resolving current user.</param>
    /// <param name="timeProvider">The time provider for timestamps.</param>
    public AuditStampFactory(IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public AuditStamp CreateStamp()
    {
        UserId actorId = ResolveCurrentUserId();
        DateTimeOffset timestamp = _timeProvider.GetUtcNow();

        // Try to get correlation ID from HTTP context
        string? correlationId = _httpContextAccessor.HttpContext?.TraceIdentifier;

        return new AuditStamp(actorId, timestamp, correlationId);
    }

    private UserId ResolveCurrentUserId()
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
        {
            // Return a system user ID for unauthenticated operations (e.g., migrations, background jobs)
            return new UserId(Guid.Empty);
        }

        // Try to get the user ID from the 'oid' claim (Azure AD object ID)
        string? oidClaim = user.FindFirst("oid")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(oidClaim, out Guid userId))
        {
            return new UserId(userId);
        }

        // Fallback to system user if no valid ID found
        return new UserId(Guid.Empty);
    }
}
