using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using System;
using System.Reflection;
using System.Security.Claims;

namespace Menlo.Application.Common;

/// <summary>
/// Attempts to resolve IHttpContextAccessor via reflection to avoid a hard dependency on ASP.NET packages.
/// Falls back to a system actor (Guid.Empty) when no HTTP context or user is available.
/// </summary>
public sealed class ReflectiveAuditStampFactory : IAuditStampFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ReflectiveAuditStampFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public AuditStamp CreateStamp()
    {
        // Try to load the IHttpContextAccessor type from the common ASP.NET assembly name if present.
        Type? accessorType = Type.GetType("Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.Abstractions");
        if (accessorType != null)
        {
            object? accessor = _serviceProvider.GetService(accessorType);
            if (accessor != null)
            {
                // Use reflection to access HttpContext property and then User.Claims
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

        // No HTTP user available — return system actor
        return new AuditStamp(new UserId(Guid.Empty), DateTimeOffset.UtcNow);
    }
}
