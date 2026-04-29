using Menlo.Application.Auth;
using Menlo.Application.Budget;
using Menlo.Application.Common.ValueConverters;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Common;

public sealed class MenloDbContext(DbContextOptions<MenloDbContext> options)
    : DbContext(options), IUserContext, IHouseholdContext, IBudgetContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<BudgetAggregate> Budgets => Set<BudgetAggregate>();
    public DbSet<CategoryNode> BudgetCategories => Set<CategoryNode>();
    public DbSet<CanonicalCategory> CanonicalCategories => Set<CanonicalCategory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MenloDbContext).Assembly);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
            BinaryExpression body = Expression.Equal(
                Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
                Expression.Constant(false));
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(body, parameter));
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserIdValueConverter>();

        configurationBuilder
            .Properties<HouseholdId>()
            .HaveConversion<HouseholdIdValueConverter>();

        configurationBuilder
            .Properties<BudgetId>()
            .HaveConversion<BudgetIdValueConverter>();

        configurationBuilder
            .Properties<BudgetCategoryId>()
            .HaveConversion<BudgetCategoryIdValueConverter>();

        configurationBuilder
            .Properties<CanonicalCategoryId>()
            .HaveConversion<CanonicalCategoryIdValueConverter>();
    }
}


