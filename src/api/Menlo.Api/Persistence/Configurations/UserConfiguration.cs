using Menlo.Api.Persistence.Converters;
using Menlo.Lib.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Api.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for the User entity.
/// Maps to the auth.users table in PostgreSQL.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table mapping with schema
        builder.ToTable("users", "auth");

        // Primary key
        builder.HasKey(u => u.Id);

        // Property configurations
        builder.Property(u => u.Id)
            .HasConversion<UserIdConverter>()
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(u => u.ExternalId)
            .HasConversion<ExternalUserIdConverter>()
            .HasColumnName("external_id")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .HasColumnName("last_login_at");

        // Audit columns
        builder.Property(u => u.CreatedBy)
            .HasConversion<NullableUserIdConverter>()
            .HasColumnName("created_by");

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(u => u.ModifiedBy)
            .HasConversion<NullableUserIdConverter>()
            .HasColumnName("modified_by");

        builder.Property(u => u.ModifiedAt)
            .HasColumnName("modified_at");

        // Indexes
        builder.HasIndex(u => u.ExternalId)
            .HasDatabaseName("ix_users_external_id")
            .IsUnique();

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("ix_users_email")
            .IsUnique();

        // Ignore domain events (not persisted)
        builder.Ignore(u => u.DomainEvents);
    }
}
