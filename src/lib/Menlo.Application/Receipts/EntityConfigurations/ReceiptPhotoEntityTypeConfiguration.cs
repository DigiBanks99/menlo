using Menlo.Lib.Receipts.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Receipts.EntityConfigurations;

public sealed class ReceiptPhotoEntityTypeConfiguration : IEntityTypeConfiguration<ReceiptPhoto>
{
    public void Configure(EntityTypeBuilder<ReceiptPhoto> builder)
    {
        builder.ToTable("receipt_photos", "financial");

        builder.HasKey(p => new { p.ReceiptId, p.Position });

        builder.Property(p => p.Position)
            .ValueGeneratedNever();

        // The photo itself, as bytea: photos live in the database so they survive restarts and are backed up with it
        builder.Property(p => p.Jpeg)
            .IsRequired();
    }
}
