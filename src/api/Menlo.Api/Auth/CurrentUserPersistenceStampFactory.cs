using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using System.Security.Claims;

namespace Menlo.Api.Auth;

internal sealed class CurrentUserPersistenceStampFactory(IHttpContextAccessor httpContextAccessor)
    : IAuditStampFactory, ISoftDeleteStampFactory
{
    public AuditStamp CreateStamp() => new(GetCurrentUserId(), DateTimeOffset.UtcNow);

    SoftDeleteStamp ISoftDeleteStampFactory.CreateStamp() => new(GetCurrentUserId(), DateTimeOffset.UtcNow);

    private UserId GetCurrentUserId()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        string? value = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user?.FindFirstValue("oid")
                        ?? user?.FindFirstValue("sub");

        return !Guid.TryParse(value, out Guid parsedUserId)
            ? throw new InvalidOperationException(
                "A valid authenticated user identifier claim is required to create persistence audit stamps.")
            : new UserId(parsedUserId);
    }
}


