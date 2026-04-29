using Menlo.Application.Budget;
using Menlo.Application.Common;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.ValueObjects;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BudgetAggregate = Menlo.Lib.Budget.Entities.Budget;

namespace Menlo.Application.Tests.Budget;

[Collection("Persistence")]
public sealed class CategoryPersistenceIntegrationTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task SaveChangesAsync_WithCanonicalCategory_PersistsCorrectly()
    {
        CanonicalCategory canonical = await CreateAndPersistCanonicalCategoryAsync("Groceries");

        CanonicalCategory? loaded = await LoadCanonicalCategoryAsync(canonical.Id);

        ItShouldHavePersistedCanonicalCategory(loaded, canonical);
        ItShouldHaveAuditFieldsOnCanonicalCategory(loaded);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCategoryNodeAllFields_PersistsCorrectly()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2040,
            auditStampFactory).Value;

        CSharpFunctionalExtensions.Result<CategoryNode, Menlo.Lib.Budget.Errors.BudgetError> result = budget.AddCategory(
            "Salary",
            BudgetFlow.Income,
            description: "Monthly salary payments",
            attribution: Attribution.Main,
            incomeContributor: "John Doe",
            responsiblePayer: null);

        CategoryNode category = result.Value;

        // Create canonical category for FK
        ctx.CanonicalCategories.Add(CanonicalCategory.Create(category.CanonicalCategoryId, category.Name.Value));
        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - load in a fresh scope
        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? loaded = await readCtx.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        // Assert
        CategoryNode? loadedCategory = loaded?.Categories.SingleOrDefault();
        ItShouldHavePersistedCategoryNode(loadedCategory, category);
        ItShouldHavePersistedCategoryEnrichments(loadedCategory);
        ItShouldHaveAuditFieldsOnCategoryNode(loadedCategory);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCategoryNodeExpenseAttribution_PersistsCorrectly()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2041,
            auditStampFactory).Value;

        CSharpFunctionalExtensions.Result<CategoryNode, Menlo.Lib.Budget.Errors.BudgetError> result = budget.AddCategory(
            "Rental Income",
            BudgetFlow.Expense,
            attribution: Attribution.Rental,
            responsiblePayer: "Jane Smith");

        CategoryNode category = result.Value;

        ctx.CanonicalCategories.Add(CanonicalCategory.Create(category.CanonicalCategoryId, category.Name.Value));
        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? loaded = await readCtx.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        // Assert
        CategoryNode? loadedCategory = loaded?.Categories.SingleOrDefault();
        loadedCategory.ShouldNotBeNull();
        loadedCategory.BudgetFlow.ShouldBe(BudgetFlow.Expense);
        loadedCategory.Attribution.ShouldBe(Attribution.Rental);
        loadedCategory.ResponsiblePayer.ShouldBe("Jane Smith");
        loadedCategory.IncomeContributor.ShouldBeNull();
        loadedCategory.Description.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithChildCategory_PersistsParentIdCorrectly()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2042,
            auditStampFactory).Value;

        CategoryNode parent = budget.AddCategory("Expenses", BudgetFlow.Expense).Value;
        CategoryNode child = budget.AddCategory("Groceries", BudgetFlow.Expense, parentId: parent.Id).Value;

        // Create canonical categories for FK
        foreach (CategoryNode cat in budget.Categories)
        {
            ctx.CanonicalCategories.Add(CanonicalCategory.Create(cat.CanonicalCategoryId, cat.Name.Value));
        }

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? loaded = await readCtx.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Categories.Count.ShouldBe(2);

        CategoryNode? loadedChild = loaded.Categories.SingleOrDefault(c => c.Name.Value == "Groceries");
        loadedChild.ShouldNotBeNull();
        loadedChild.ParentId.ShouldBe(parent.Id);
    }

    [Fact]
    public async Task SaveChangesAsync_CategoryNodeWithNonExistentCanonicalCategoryId_ThrowsFkViolation()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2043,
            auditStampFactory).Value;

        // Add category but DO NOT create the canonical category
        budget.AddCategory("Orphaned", BudgetFlow.Expense);
        ctx.Budgets.Add(budget);

        // Act & Assert
        Func<Task> act = () => ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        await act.ShouldThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateCategoryNameSameBudgetAndParent_ThrowsUniqueConstraint()
    {
        // Arrange - create a budget with a parent and child category
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2044,
            auditStampFactory).Value;

        CategoryNode parent = budget.AddCategory("Expenses", BudgetFlow.Expense).Value;
        budget.AddCategory("Food", BudgetFlow.Expense, parentId: parent.Id);

        foreach (CategoryNode cat in budget.Categories)
        {
            ctx.CanonicalCategories.Add(CanonicalCategory.Create(cat.CanonicalCategoryId, cat.Name.Value));
        }

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Now insert a duplicate child with same (budget_id, parent_id, name) via raw SQL
        await using AsyncServiceScope scope2 = fixture.Services.CreateAsyncScope();
        MenloDbContext dbContext = scope2.ServiceProvider.GetRequiredService<MenloDbContext>();

        CanonicalCategoryId newCanonicalId = CanonicalCategoryId.NewId();
        dbContext.CanonicalCategories.Add(CanonicalCategory.Create(newCanonicalId, "Food Duplicate"));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Use raw SQL to insert a duplicate child (same budget_id, same parent_id, same name)
        Func<Task> act = () => dbContext.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO budget_schema.budget_categories 
               (id, budget_id, parent_id, name, canonical_category_id, budget_flow, is_deleted)
               VALUES ({Guid.NewGuid()}, {budget.Id.Value}, {parent.Id.Value}, {"Food"}, {newCanonicalId.Value}, {"Expense"}, {false})");

        // Act & Assert - unique constraint violation
        Exception ex = await Should.ThrowAsync<Exception>(act);
        // Npgsql wraps as PostgresException; verify it's a unique violation (23505)
        ex.ShouldBeAssignableTo<Npgsql.PostgresException>();
        ((Npgsql.PostgresException)ex).SqlState.ShouldBe("23505");
    }

    [Fact]
    public async Task SaveChangesAsync_SameCategoryNameWhenOneIsSoftDeleted_Succeeds()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();
        ISoftDeleteStampFactory softDeleteStampFactory = scope.ServiceProvider.GetRequiredService<ISoftDeleteStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2045,
            auditStampFactory).Value;

        CategoryNode original = budget.AddCategory("Transport", BudgetFlow.Expense).Value;

        foreach (CategoryNode cat in budget.Categories)
        {
            ctx.CanonicalCategories.Add(CanonicalCategory.Create(cat.CanonicalCategoryId, cat.Name.Value));
        }

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Soft-delete the original
        using IServiceScope deleteScope = fixture.Services.CreateScope();
        IBudgetContext deleteCtx = deleteScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? toDelete = await deleteCtx.Budgets
            .Include(b => b.Categories)
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        toDelete.ShouldNotBeNull();
        toDelete.SoftDeleteCategory(original.Id, softDeleteStampFactory);
        await deleteCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Now add a new category with the same name (should succeed because the filtered index excludes deleted)
        using IServiceScope addScope = fixture.Services.CreateScope();
        IBudgetContext addCtx = addScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? toAdd = await addCtx.Budgets
            .Include(b => b.Categories)
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        toAdd.ShouldNotBeNull();
        CategoryNode replacement = toAdd.AddCategory("Transport", BudgetFlow.Expense).Value;
        addCtx.CanonicalCategories.Add(CanonicalCategory.Create(replacement.CanonicalCategoryId, replacement.Name.Value));

        // Act - should not throw
        await addCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - verify both exist (one deleted, one active)
        using IServiceScope verifyScope = fixture.Services.CreateScope();
        IBudgetContext verifyCtx = verifyScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        List<CategoryNode> allCategories = await verifyCtx.BudgetCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => EF.Property<BudgetId>(c, "budget_id") == budget.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        ItShouldHaveBothDeletedAndActiveCategories(allCategories);
    }

    [Fact]
    public async Task Query_WithSoftDeletedCategory_FiltersSoftDeletedFromResults()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();
        ISoftDeleteStampFactory softDeleteStampFactory = scope.ServiceProvider.GetRequiredService<ISoftDeleteStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2046,
            auditStampFactory).Value;

        CategoryNode toDelete = budget.AddCategory("Old Category", BudgetFlow.Expense).Value;
        budget.AddCategory("Active Category", BudgetFlow.Income);

        foreach (CategoryNode cat in budget.Categories)
        {
            ctx.CanonicalCategories.Add(CanonicalCategory.Create(cat.CanonicalCategoryId, cat.Name.Value));
        }

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Soft-delete one category
        using IServiceScope deleteScope = fixture.Services.CreateScope();
        IBudgetContext deleteCtx = deleteScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? loaded = await deleteCtx.Budgets
            .Include(b => b.Categories)
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.SoftDeleteCategory(toDelete.Id, softDeleteStampFactory);
        await deleteCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - read with query filters
        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? result = await readCtx.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        ItShouldOnlyReturnActiveCategories(result);
    }

    [Fact]
    public async Task Query_WithSoftDeletedCategoryIgnoringFilters_ReturnsSoftDeletedWithMetadata()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();
        ISoftDeleteStampFactory softDeleteStampFactory = scope.ServiceProvider.GetRequiredService<ISoftDeleteStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2047,
            auditStampFactory).Value;

        CategoryNode toDelete = budget.AddCategory("Deletable", BudgetFlow.Expense).Value;

        foreach (CategoryNode cat in budget.Categories)
        {
            ctx.CanonicalCategories.Add(CanonicalCategory.Create(cat.CanonicalCategoryId, cat.Name.Value));
        }

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Soft-delete
        using IServiceScope deleteScope = fixture.Services.CreateScope();
        IBudgetContext deleteCtx = deleteScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? loaded = await deleteCtx.Budgets
            .Include(b => b.Categories)
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        loaded.ShouldNotBeNull();
        loaded.SoftDeleteCategory(toDelete.Id, softDeleteStampFactory);
        await deleteCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - read with IgnoreQueryFilters
        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        CategoryNode? deleted = await readCtx.BudgetCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == toDelete.Id, TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveSoftDeleteMetadata(deleted);
    }

    [Fact]
    public async Task SaveChangesAsync_AuditInterceptorOnCategoryNode_SetsCreatedAndModifiedFields()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        IAuditStampFactory auditStampFactory = scope.ServiceProvider.GetRequiredService<IAuditStampFactory>();

        BudgetAggregate budget = BudgetAggregate.Create(
            HouseholdId.NewId(),
            2048,
            auditStampFactory).Value;

        budget.AddCategory("Audited", BudgetFlow.Income);

        foreach (CategoryNode cat in budget.Categories)
        {
            ctx.CanonicalCategories.Add(CanonicalCategory.Create(cat.CanonicalCategoryId, cat.Name.Value));
        }

        ctx.Budgets.Add(budget);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - read
        using IServiceScope readScope = fixture.Services.CreateScope();
        IBudgetContext readCtx = readScope.ServiceProvider.GetRequiredService<IBudgetContext>();
        BudgetAggregate? loaded = await readCtx.Budgets
            .Include(b => b.Categories)
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == budget.Id, TestContext.Current.CancellationToken);

        // Assert
        CategoryNode? category = loaded?.Categories.SingleOrDefault();
        ItShouldHaveAuditFieldsOnCategoryNode(category);
    }

    [Fact]
    public async Task SaveChangesAsync_AuditInterceptorOnCanonicalCategory_SetsCreatedAndModifiedFields()
    {
        CanonicalCategory canonical = await CreateAndPersistCanonicalCategoryAsync("Audited Canonical");

        CanonicalCategory? loaded = await LoadCanonicalCategoryAsync(canonical.Id);

        ItShouldHaveAuditFieldsOnCanonicalCategory(loaded);
    }

    // --- Helper methods ---

    private async Task<CanonicalCategory> CreateAndPersistCanonicalCategoryAsync(string name)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();

        CanonicalCategory canonical = CanonicalCategory.Create(CanonicalCategoryId.NewId(), name);
        ctx.CanonicalCategories.Add(canonical);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        return canonical;
    }

    private async Task<CanonicalCategory?> LoadCanonicalCategoryAsync(CanonicalCategoryId id)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext ctx = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        return await ctx.CanonicalCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, TestContext.Current.CancellationToken);
    }

    // --- Assertion helpers ---

    private static void ItShouldHavePersistedCanonicalCategory(CanonicalCategory? loaded, CanonicalCategory original)
    {
        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(original.Id);
        loaded.Name.ShouldBe(original.Name);
    }

    private static void ItShouldHaveAuditFieldsOnCanonicalCategory(CanonicalCategory? loaded)
    {
        loaded.ShouldNotBeNull();
        loaded.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        loaded.CreatedAt.ShouldNotBeNull();
        loaded.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        loaded.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldHavePersistedCategoryNode(CategoryNode? loaded, CategoryNode original)
    {
        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(original.Id);
        loaded.Name.Value.ShouldBe(original.Name.Value);
        loaded.CanonicalCategoryId.ShouldBe(original.CanonicalCategoryId);
        loaded.BudgetFlow.ShouldBe(original.BudgetFlow);
        loaded.ParentId.ShouldBe(original.ParentId);
    }

    private static void ItShouldHavePersistedCategoryEnrichments(CategoryNode? loaded)
    {
        loaded.ShouldNotBeNull();
        loaded.Attribution.ShouldBe(Attribution.Main);
        loaded.Description.ShouldBe("Monthly salary payments");
        loaded.IncomeContributor.ShouldBe("John Doe");
        loaded.ResponsiblePayer.ShouldBeNull();
    }

    private static void ItShouldHaveAuditFieldsOnCategoryNode(CategoryNode? loaded)
    {
        loaded.ShouldNotBeNull();
        loaded.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        loaded.CreatedAt.ShouldNotBeNull();
        loaded.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        loaded.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldHaveBothDeletedAndActiveCategories(List<CategoryNode> allCategories)
    {
        allCategories.Count.ShouldBe(2);
        allCategories.Count(c => c.IsDeleted).ShouldBe(1);
        allCategories.Count(c => !c.IsDeleted).ShouldBe(1);
        allCategories.ShouldAllBe(c => c.Name.Value == "Transport");
    }

    private static void ItShouldOnlyReturnActiveCategories(BudgetAggregate result)
    {
        result.Categories.Count.ShouldBe(1);
        result.Categories.Single().Name.Value.ShouldBe("Active Category");
    }

    private static void ItShouldHaveSoftDeleteMetadata(CategoryNode? deleted)
    {
        deleted.ShouldNotBeNull();
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedAt.ShouldNotBeNull();
        deleted.DeletedBy.ShouldBe(TestAuditStampFactory.TestUserId);
    }
}
