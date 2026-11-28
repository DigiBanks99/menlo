using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Receipts.Entities;
using Menlo.Lib.Receipts.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Menlo.Application.Receipts.EntityConfigurations;

public sealed class ReceiptEntityTypeConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("receipts", "financial");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.Year)
            .IsRequired();

        builder.Property(r => r.TakenBy)
            .IsRequired();

        builder.Property(r => r.TakenAt)
            .IsRequired();

        builder.Property(r => r.BudgetView)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Attempts)
            .HasDefaultValue(0)
            .IsRequired();

        // What was read, as JSONB: it is only ever read and replaced whole
        builder.Property(r => r.Reading)
            .HasColumnType("jsonb")
            .HasConversion(
                reading => JsonSerializer.Serialize(ReadingDto.From(reading!), JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<ReadingDto>(json, JsonSerializerOptions.Default)!.ToReading())
            .IsRequired(false);

        builder.HasMany(r => r.Photos)
            .WithOne()
            .HasForeignKey(p => p.ReceiptId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Photos)
            .HasField("_photos")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(r => r.CreatedBy)
            .IsRequired(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired(false);

        builder.Property(r => r.ModifiedBy)
            .IsRequired(false);

        builder.Property(r => r.ModifiedAt)
            .IsRequired(false);

        // The list for a year, and the reading worker taking the oldest waiting receipt
        builder.HasIndex(r => new { r.Year, r.TakenAt })
            .HasDatabaseName("ix_receipts_year_taken_at");

        builder.HasIndex(r => new { r.Status, r.TakenAt })
            .HasDatabaseName("ix_receipts_status_taken_at");
    }

    private sealed record ReadingDto(
        MoneyDto? Total,
        DateOnly? Date,
        string? Merchant,
        bool? IsRefund,
        List<LineDto> Lines,
        int? UnreadLines,
        bool? SawMoreThanOneTotal)
    {
        public static ReadingDto From(ReceiptReading reading) => new(
            MoneyDto.From(reading.Total),
            reading.Date,
            reading.Merchant,
            reading.IsRefund,
            [.. reading.Lines.Select(line => new LineDto(line.Description, MoneyDto.From(line.Amount)))],
            reading.UnreadLines,
            reading.SawMoreThanOneTotal);

        public ReceiptReading ToReading() => new(
            Total?.ToMoney(),
            Date,
            Merchant,
            IsRefund,
            [.. (Lines ?? []).Select(line => new ReceiptLine(line.Description, line.Amount?.ToMoney()))],
            UnreadLines,
            SawMoreThanOneTotal);
    }

    private sealed record LineDto(string? Description, MoneyDto? Amount);

    private sealed record MoneyDto(decimal Amount, string Currency)
    {
        public static MoneyDto? From(Money? money) =>
            money is { } value ? new MoneyDto(value.Amount, value.Currency) : null;

        public Money ToMoney() => Money.Create(Amount, Currency).Value;
    }
}
