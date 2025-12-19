using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Factory for creating AuditStamp instances from infrastructure concerns.
/// The domain depends only on this interface. Concrete implementations
/// (adapting ClaimsPrincipal, TimeProvider, etc.) are provided by the
/// infrastructure/application layer in a separate requirement.
/// </summary>
public interface IAuditStampFactory
{
    /// <summary>
    /// Creates an AuditStamp for the current user and time.
    /// </summary>
    /// <returns>An AuditStamp containing the current actor, timestamp, and optional correlation ID.</returns>
    AuditStamp CreateStamp();
}
