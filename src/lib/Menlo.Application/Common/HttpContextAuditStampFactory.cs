using System;
using System.Reflection;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using System.Security.Claims;

namespace Menlo.Application.Common;

// Backwards-compatible name, but implemented via reflection to avoid an ASP.NET compile dependency.
public sealed class HttpContextAuditStampFactory : IAuditStampFactory
{
    private readonly IServiceProvider _serviceProvider;

    public HttpContextAuditStampFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public AuditStamp CreateStamp()
    {
        // Attempt to resolve IHttpContextAccessor via reflection; falls back to system actor when unavailable.
        Type? accessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions");
        if (accessorType != null)
        {
            object? accessor = _serviceProvider.GetService(accessorType);
            if (accessor != null)
            {
                PropertyInfo? httpContextProp = accessorType.GetProperty("HttpContext");
                object? httpContext = httpContextProp?.GetValue(accessor);
                if (httpContext != null)
                {
                    PropertyInfo? userProp = httpContext.GetType().GetProperty("User");
                    object? userObj = userProp?.GetValue(httpContext);
                    if (userObj is ClaimsPrincipal principal)
                    {
                        Claim? idClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
                        if (idClaim != null && Guid.TryParse(idClaim.Value, out Guid guid))
                        {
                            return new AuditStamp(new UserId(guid), DateTimeOffset.UtcNow);
                        }
                    }
                }
            }
        }

        return new AuditStamp(new UserId(Guid.Empty), DateTimeOffset.UtcNow);
    }
}
