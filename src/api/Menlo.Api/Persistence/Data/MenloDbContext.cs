using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Budget.Entities;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Api.Persistence.Data;

/// <summary>
/// Entity Framework Core database context for the Menlo application.
/// Uses schema separation for domain boundaries following DDD principles.
/// </summary>
public sealed class MenloDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MenloDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public MenloDbContext(DbContextOptions<MenloDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet (auth schema).
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets or sets the Budgets DbSet (budget schema).
    /// </summary>
    public DbSet<Budget> Budgets => Set<Budget>();

    /// <summary>
    /// Gets or sets the BudgetCategories DbSet (budget schema).
    /// </summary>
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenloDbContext).Assembly);
    }
}
