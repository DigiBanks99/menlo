using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Api.Persistence.Converters;

/// <summary>
/// EF Core value converter for UserId strongly-typed ID.
/// Converts between UserId and Guid for database storage.
/// </summary>
public sealed class UserIdConverter : ValueConverter<UserId, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserIdConverter"/> class.
    /// </summary>
    public UserIdConverter() : base(
        id => id.Value,
        guid => new UserId(guid))
    {
    }
}

/// <summary>
/// EF Core value converter for nullable UserId.
/// </summary>
public sealed class NullableUserIdConverter : ValueConverter<UserId?, Guid?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NullableUserIdConverter"/> class.
    /// </summary>
    public NullableUserIdConverter() : base(
        id => id.HasValue ? id.Value.Value : null,
        guid => guid.HasValue ? new UserId(guid.Value) : null)
    {
    }
}
