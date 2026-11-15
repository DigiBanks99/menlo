using Menlo.Lib.Auth.Models;

namespace Menlo.Application.Auth;

/// <summary>
/// The signed-in user for the current scope, as the request middleware recorded it. It is the one thing the stamp
/// factories need, and it needs nothing else (no DbContext), so it can be read while the DbContext is being built.
/// <see cref="UserContextProvider"/> is the lookup on top of it.
/// </summary>
public sealed class CurrentUser
{
    /// <summary>The recorded user, or <c>null</c> when no signed-in user is known.</summary>
    public UserContext? Value { get; private set; }

    /// <summary>Records the signed-in user for the rest of this scope. Set once, before the stamp factories are built.</summary>
    public void Set(UserContext userContext)
    {
        if (Value is not null)
        {
            throw new InvalidOperationException("The current user is already set for this scope.");
        }

        Value = userContext;
    }
}
