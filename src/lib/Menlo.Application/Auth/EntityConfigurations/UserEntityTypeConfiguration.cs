using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Auth.EntityConfigurations;

public sealed class UserEntityTypeConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "shared");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.ExternalId)
            .HasConversion(
                externalUserId => externalUserId.Value,
                value => new ExternalUserId(value))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(user => user.CreatedBy)
            .IsRequired(false);

        builder.Property(user => user.CreatedAt)
            .IsRequired(false);

        builder.Property(user => user.ModifiedBy)
            .IsRequired(false);

        builder.Property(user => user.ModifiedAt)
            .IsRequired(false);

        // Soft-delete properties
        builder.Property(user => user.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(user => user.DeletedAt)
            .IsRequired(false);

        builder.Property(user => user.DeletedBy)
            .IsRequired(false);

        builder.HasIndex(user => user.ExternalId)
            .IsUnique();

        builder.Ignore(user => user.DomainEvents);
    }
}


