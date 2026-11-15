using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Common;

/// <summary>
/// The actor behind changes nobody signed in for: seeding, background work and requests with no known user.
/// </summary>
public static class SystemUser
{
    /// <summary>The one system <see cref="UserId"/>.</summary>
    public static readonly UserId Id = new(Guid.Parse("00000000-0000-0000-0000-000000000001"));
}
