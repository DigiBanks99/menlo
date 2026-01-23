using Menlo.Lib.Auth.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Menlo.Api.Persistence.Converters;

/// <summary>
/// EF Core value converter for ExternalUserId strongly-typed ID.
/// Converts between ExternalUserId and string for database storage.
/// </summary>
public sealed class ExternalUserIdConverter : ValueConverter<ExternalUserId, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalUserIdConverter"/> class.
    /// </summary>
    public ExternalUserIdConverter() : base(
        id => id.Value,
        value => new ExternalUserId(value))
    {
    }
}
