using Menlo.Application.Common.ValueConverters;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Tests.TestHelpers;

/// <summary>
/// Test-only DbContext with soft-deletable and audit-only entities.
/// Registers its own soft-delete query filter so interceptor tests stay realistic.
/// Uses <see cref="UserIdValueConverter"/> explicitly to ensure converter coverage.
/// </summary>
internal sealed class TestMenloDbContext : DbContext
{
    public TestMenloDbContext(DbContextOptions<TestMenloDbContext> options) : base(options) { }

    public DbSet<TestSoftDeletableEntity> TestEntities => Set<TestSoftDeletableEntity>();
    public DbSet<TestAuditOnlyEntity> AuditOnlyEntities => Set<TestAuditOnlyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestSoftDeletableEntity>(entity =>
        {
            entity.ToTable("test_soft_deletable_entities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasConversion(new UserIdValueConverter());
            entity.Property(e => e.CreatedBy)
                .HasConversion(id => id!.Value.Value, guid => new UserId(guid))
                .IsRequired(false);
            entity.Property(e => e.ModifiedBy)
                .HasConversion(id => id!.Value.Value, guid => new UserId(guid))
                .IsRequired(false);
            entity.Property(e => e.DeletedBy)
                .HasConversion(id => id!.Value.Value, guid => new UserId(guid))
                .IsRequired(false);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<TestAuditOnlyEntity>(entity =>
        {
            entity.ToTable("test_audit_only_entities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasConversion(new UserIdValueConverter());
            entity.Property(e => e.CreatedBy)
                .HasConversion(id => id!.Value.Value, guid => new UserId(guid))
                .IsRequired(false);
            entity.Property(e => e.ModifiedBy)
                .HasConversion(id => id!.Value.Value, guid => new UserId(guid))
                .IsRequired(false);
        });
    }
}
