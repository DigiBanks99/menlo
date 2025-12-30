namespace Menlo.Api.Auth.Policies;

/// <summary>
/// Static class containing policy names and role constants for authorization.
/// </summary>
public static class MenloPolicies
{
    /// <summary>
    /// Policy requiring the user to be authenticated.
    /// </summary>
    public const string RequireAuthenticated = "RequireAuthenticated";

    /// <summary>
    /// Policy requiring the Admin role.
    /// </summary>
    public const string RequireAdmin = "RequireAdmin";

    /// <summary>
    /// Policy allowing budget editing (Admin and User roles).
    /// </summary>
    public const string CanEditBudget = "CanEditBudget";

    /// <summary>
    /// Policy allowing budget viewing (Admin, User, and Reader roles).
    /// </summary>
    public const string CanViewBudget = "CanViewBudget";

    /// <summary>
    /// Role values that must match Entra ID App Roles.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Administrator role with full access.
        /// </summary>
        public const string Admin = "Menlo.Admin";

        /// <summary>
        /// Standard user role with read/write access.
        /// </summary>
        public const string User = "Menlo.User";

        /// <summary>
        /// Read-only role with view access.
        /// </summary>
        public const string Reader = "Menlo.Reader";
    }
}
