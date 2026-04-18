using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Menlo.Application.Budget.EntityConfigurations;

public sealed class CategoryNodeEntityTypeConfiguration : IEntityTypeConfiguration<CategoryNode>
{
    public void Configure(EntityTypeBuilder<CategoryNode> builder)
    {
        builder.ToTable("budget_categories", "budget_schema");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        builder.Property(n => n.Name)
            .HasConversion(
                name => name.Value,
                value => CategoryName.Create(value).Value)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.ParentId)
            .IsRequired(false);

        builder.ComplexProperty(n => n.PlannedMonthlyAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("planned_amount")
                .HasColumnType("numeric(18,2)");

            money.Property(m => m.Currency)
                .HasColumnName("planned_currency")
                .HasMaxLength(3);
        });

        builder.HasOne<CategoryNode>()
            .WithMany()
            .HasForeignKey(n => n.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
