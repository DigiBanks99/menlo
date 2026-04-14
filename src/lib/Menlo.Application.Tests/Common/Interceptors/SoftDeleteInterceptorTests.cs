using Menlo.Application.Tests.Fixtures;
using Menlo.Application.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Common.Interceptors;

[Collection("Interceptors")]
public sealed class SoftDeleteInterceptorTests(InterceptorFixture fixture)
{
    [Fact]
    public async Task GivenSoftDeletableEntity_WhenRemoved_ThenEntityIsSoftDeleted()
    {
        // Arrange
        using IServiceScope addScope = fixture.Services.CreateScope();
        TestMenloDbContext addCtx = addScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity entity = TestSoftDeletableEntity.Create();
        addCtx.TestEntities.Add(entity);
        await addCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - remove in a new scope
        using IServiceScope removeScope = fixture.Services.CreateScope();
        TestMenloDbContext removeCtx = removeScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity? toRemove = await removeCtx.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);
        toRemove.ShouldNotBeNull();
        removeCtx.TestEntities.Remove(toRemove);
        await removeCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - entity is soft-deleted, not hard-deleted
        ItShouldBeSoftDeleted(toRemove);
    }

    [Fact]
    public async Task GivenSoftDeletedEntity_WhenQuerying_ThenEntityIsFilteredOut()
    {
        // Arrange - create and soft-delete an entity
        using IServiceScope addScope = fixture.Services.CreateScope();
        TestMenloDbContext addCtx = addScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity entity = TestSoftDeletableEntity.Create();
        addCtx.TestEntities.Add(entity);
        await addCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        using IServiceScope removeScope = fixture.Services.CreateScope();
        TestMenloDbContext removeCtx = removeScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity? toRemove = await removeCtx.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);
        toRemove.ShouldNotBeNull();
        removeCtx.TestEntities.Remove(toRemove);
        await removeCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - query without ignoring filters
        using IServiceScope queryScope = fixture.Services.CreateScope();
        TestMenloDbContext queryCtx = queryScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        bool exists = await queryCtx.TestEntities
            .AnyAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);

        // Assert
        ItShouldNotBeVisible(exists);
    }

    [Fact]
    public async Task GivenSoftDeletedEntity_WhenQueryingIgnoringFilters_ThenEntityIsFound()
    {
        // Arrange - create and soft-delete
        using IServiceScope addScope = fixture.Services.CreateScope();
        TestMenloDbContext addCtx = addScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity entity = TestSoftDeletableEntity.Create();
        addCtx.TestEntities.Add(entity);
        await addCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        using IServiceScope removeScope = fixture.Services.CreateScope();
        TestMenloDbContext removeCtx = removeScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity? toRemove = await removeCtx.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);
        toRemove.ShouldNotBeNull();
        removeCtx.TestEntities.Remove(toRemove);
        await removeCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - query ignoring filters
        using IServiceScope queryScope = fixture.Services.CreateScope();
        TestMenloDbContext queryCtx = queryScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity? found = await queryCtx.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);

        // Assert
        ItShouldExistWithSoftDeleteMarkings(found);
    }

    private static void ItShouldBeSoftDeleted(TestSoftDeletableEntity entity)
    {
        entity.IsDeleted.ShouldBeTrue();
        entity.DeletedAt.ShouldNotBeNull();
        entity.DeletedBy.ShouldNotBeNull();
    }

    private static void ItShouldNotBeVisible(bool exists)
    {
        exists.ShouldBeFalse("Soft-deleted entity should be filtered out by global query filter");
    }

    private static void ItShouldExistWithSoftDeleteMarkings(TestSoftDeletableEntity? entity)
    {
        entity.ShouldNotBeNull();
        entity.IsDeleted.ShouldBeTrue();
    }
}
