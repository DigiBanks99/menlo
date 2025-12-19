namespace Menlo.Lib.Common.Enums;

/// <summary>
/// Specifies the type of audit operation being performed.
/// </summary>
public enum AuditOperation
{
    /// <summary>
    /// The entity is being created for the first time.
    /// Sets both Created and Modified audit fields.
    /// </summary>
    Create = 0,

    /// <summary>
    /// The entity is being updated after initial creation.
    /// Updates only Modified audit fields.
    /// </summary>
    Update = 1
}
