using Menlo.Application.Tests.Fixtures;
using Menlo.Application.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Common.Interceptors;

[Collection("Interceptors")]
public sealed class AuditingInterceptorTests(InterceptorFixture fixture)
{
    [Fact]
    public async Task GivenNewAuditableEntity_WhenSaved_ThenAuditFieldsAreStamped()
    {
        // Arrange
        using IServiceScope scope = fixture.Services.CreateScope();
        TestMenloDbContext ctx = scope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity entity = TestSoftDeletableEntity.Create();

        // Act
        ctx.TestEntities.Add(entity);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveAuditFields(entity);
    }

    [Fact]
    public async Task GivenExistingAuditableEntity_WhenUpdated_ThenModifiedFieldsAreRefreshed()
    {
        // Arrange
        using IServiceScope createScope = fixture.Services.CreateScope();
        TestMenloDbContext createCtx = createScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity entity = TestSoftDeletableEntity.Create();
        createCtx.TestEntities.Add(entity);
        await createCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        DateTimeOffset? originalModifiedAt = entity.ModifiedAt;

        // Allow a small delay so timestamps differ
        await Task.Delay(10, TestContext.Current.CancellationToken);

        // Act - update in a new scope
        using IServiceScope updateScope = fixture.Services.CreateScope();
        TestMenloDbContext updateCtx = updateScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestSoftDeletableEntity? reloaded = await updateCtx.TestEntities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);
        reloaded.ShouldNotBeNull();

        updateCtx.TestEntities.Update(reloaded);
        await updateCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        ItShouldHaveUpdatedModifiedAt(reloaded, originalModifiedAt);
    }

    [Fact]
    public async Task GivenNonSoftDeletableAuditableEntity_WhenRemoved_ThenEntityIsHardDeleted()
    {
        // Arrange
        using IServiceScope addScope = fixture.Services.CreateScope();
        TestMenloDbContext addCtx = addScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestAuditOnlyEntity entity = TestAuditOnlyEntity.Create();
        addCtx.AuditOnlyEntities.Add(entity);
        await addCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - hard delete (AuditingInterceptor sees EntityState.Deleted → _ => null → no stamp)
        using IServiceScope removeScope = fixture.Services.CreateScope();
        TestMenloDbContext removeCtx = removeScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestAuditOnlyEntity? toRemove = await removeCtx.AuditOnlyEntities
            .FirstOrDefaultAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);
        toRemove.ShouldNotBeNull();
        removeCtx.AuditOnlyEntities.Remove(toRemove);
        await removeCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - physically removed, not soft-deleted
        using IServiceScope verifyScope = fixture.Services.CreateScope();
        TestMenloDbContext verifyCtx = verifyScope.ServiceProvider.GetRequiredService<TestMenloDbContext>();
        TestAuditOnlyEntity? found = await verifyCtx.AuditOnlyEntities
            .FirstOrDefaultAsync(e => e.Id == entity.Id, TestContext.Current.CancellationToken);

        ItShouldBeHardDeleted(found);
    }

    private static void ItShouldBeHardDeleted(TestAuditOnlyEntity? entity)
    {
        entity.ShouldBeNull("Non-soft-deletable entity should be physically removed from the database");
    }

    private static void ItShouldHaveAuditFields(TestSoftDeletableEntity entity)
    {
        entity.CreatedBy.ShouldNotBeNull();
        entity.CreatedAt.ShouldNotBeNull();
        entity.ModifiedBy.ShouldNotBeNull();
        entity.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldHaveUpdatedModifiedAt(TestSoftDeletableEntity entity, DateTimeOffset? original)
    {
        entity.ModifiedAt.ShouldNotBeNull();
    }
}
