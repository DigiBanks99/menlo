using Menlo.Application.Auth;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Tests.Fixtures;

internal sealed class UserContextTestDbContext(DbContextOptions<UserContextTestDbContext> options)
    : DbContext(options), IUserContext
{
    public DbSet<User> Users => Set<User>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id)
                .ValueGeneratedNever()
                .HasConversion(id => id.Value, guid => new UserId(guid));
            entity.Property(u => u.ExternalId)
                .HasConversion(id => id.Value, value => new ExternalUserId(value));
            entity.Property(u => u.HouseholdId)
                .HasConversion(
                    id => id.HasValue ? id.Value.Value : (Guid?)null,
                    guid => guid.HasValue ? (HouseholdId?)new HouseholdId(guid.Value) : null)
                .IsRequired(false);
            entity.Property(u => u.CreatedBy)
                .HasConversion(id => id.HasValue ? id.Value.Value : (Guid?)null,
                    guid => guid.HasValue ? (UserId?)new UserId(guid.Value) : null)
                .IsRequired(false);
            entity.Property(u => u.ModifiedBy)
                .HasConversion(id => id.HasValue ? id.Value.Value : (Guid?)null,
                    guid => guid.HasValue ? (UserId?)new UserId(guid.Value) : null)
                .IsRequired(false);
            entity.Property(u => u.DeletedBy)
                .HasConversion(id => id.HasValue ? id.Value.Value : (Guid?)null,
                    guid => guid.HasValue ? (UserId?)new UserId(guid.Value) : null)
                .IsRequired(false);
            entity.Ignore(u => u.DomainEvents);
        });
    }

    public static UserContextTestDbContext CreateInMemory(params User[] users)
    {
        DbContextOptions<UserContextTestDbContext> options = new DbContextOptionsBuilder<UserContextTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        UserContextTestDbContext ctx = new(options);

        if (users.Length > 0)
        {
            ctx.Users.AddRange(users);
            ctx.SaveChanges();
        }

        return ctx;
    }
}
