using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Factory for creating <see cref="SoftDeleteStamp"/> instances from infrastructure concerns.
/// Mirrors <see cref="IAuditStampFactory"/> for the soft-delete interceptor.
/// </summary>
public interface ISoftDeleteStampFactory
{
    /// <summary>Creates a stamp capturing the current actor and timestamp for a soft delete.</summary>
    SoftDeleteStamp CreateStamp();
}
