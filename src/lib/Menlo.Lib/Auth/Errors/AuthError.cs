using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Auth.Errors;

/// <summary>
/// Base class for authentication and authorisation domain errors.
/// </summary>
/// <param name="code">Machine-readable error code.</param>
/// <param name="message">Human-readable error message.</param>
public class AuthError(string code, string message) : Error(code, message);

/// <summary>
/// Error indicating a user was not found by external ID.
/// </summary>
/// <param name="externalId">The external ID that was not found.</param>
public class UserNotFoundError(string externalId)
    : AuthError("Auth.UserNotFound", $"No user found with external ID: {externalId}")
{
    /// <summary>
    /// Gets the external ID that was not found.
    /// </summary>
    public string ExternalId { get; } = externalId;
}

/// <summary>
/// Error indicating the user is not authenticated.
/// </summary>
public class UnauthenticatedError()
    : AuthError("Auth.Unauthenticated", "You are not authenticated. Please log in.");

/// <summary>
/// Error indicating the user is not authorised for an action.
/// </summary>
public class UnauthorisedError()
    : AuthError("Auth.Unauthorised", "You are not authorised to perform this action.");

/// <summary>
/// Error indicating access is forbidden due to policy requirements.
/// </summary>
/// <param name="policy">The policy that was not satisfied.</param>
public class ForbiddenError(string policy)
    : AuthError("Auth.Forbidden", $"Access denied. Required policy: {policy}")
{
    /// <summary>
    /// Gets the policy that was not satisfied.
    /// </summary>
    public string Policy { get; } = policy;
}

/// <summary>
/// Error indicating invalid user data.
/// </summary>
/// <param name="reason">The reason the data is invalid.</param>
public class InvalidUserDataError(string reason)
    : AuthError("Auth.InvalidUserData", $"Invalid user data: {reason}")
{
    /// <summary>
    /// Gets the reason the data is invalid.
    /// </summary>
    public string Reason { get; } = reason;
}

/// <summary>
/// Error indicating the user is not assigned to any household.
/// </summary>
/// <param name="externalId">The external ID of the user without a household assignment.</param>
public class UserNotAssignedToHouseholdError(string externalId)
    : AuthError("Auth.UserNotAssignedToHousehold", $"User '{externalId}' is not assigned to any household.")
{
    /// <summary>
    /// Gets the external ID of the user.
    /// </summary>
    public string ExternalId { get; } = externalId;
}

/// <summary>
/// Base class for household domain errors.
/// </summary>
/// <param name="code">Machine-readable error code.</param>
/// <param name="message">Human-readable error message.</param>
public class HouseholdError(string code, string message) : Error(code, message);

/// <summary>
/// Error indicating invalid household data.
/// </summary>
/// <param name="reason">The reason the data is invalid.</param>
public class InvalidHouseholdDataError(string reason)
    : HouseholdError("Household.InvalidData", $"Invalid household data: {reason}")
{
    /// <summary>
    /// Gets the reason the data is invalid.
    /// </summary>
    public string Reason { get; } = reason;
}


