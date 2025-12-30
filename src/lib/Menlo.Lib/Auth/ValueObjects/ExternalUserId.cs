namespace Menlo.Lib.Auth.ValueObjects;

/// <summary>
/// Represents the external identity provider's user identifier (e.g., Entra ID Object ID / oid claim).
/// This is the unique identifier from the external IdP, not our internal UserId.
/// </summary>
/// <param name="Value">The external provider's unique identifier for the user.</param>
public readonly record struct ExternalUserId(string Value)
{
    /// <summary>
    /// Returns the string representation of the external user ID.
    /// </summary>
    public override string ToString() => Value;
}
