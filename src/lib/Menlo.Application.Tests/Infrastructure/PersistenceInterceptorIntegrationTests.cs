using Menlo.Application.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Tests.Infrastructure;

[Collection("InterceptorPersistence")]
public sealed class PersistenceInterceptorIntegrationTests(InterceptorPersistenceFixture fixture)
{
    [Fact]
    public async Task SaveChangesAsync_WithNewAuditableEntity()
    {
        await ResetDatabaseAsync();

        await using InterceptorTestDbContext writeContext = CreateContext();
        TestAuditableEntity entity = new()
        {
            Name = "New entity"
        };

        writeContext.AuditableEntities.Add(entity);
        await writeContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using InterceptorTestDbContext readContext = CreateContext();
        TestAuditableEntity? persistedEntity = await readContext.AuditableEntities
            .SingleOrDefaultAsync(x => x.Id == entity.Id, TestContext.Current.CancellationToken);

        ItShouldHavePersistedAuditableEntity(persistedEntity);
        ItShouldHaveCreatedAuditFields(persistedEntity);
        ItShouldHaveModifiedAuditFields(persistedEntity);
    }

    [Fact]
    public async Task SaveChangesAsync_WithModifiedAuditableEntity()
    {
        await ResetDatabaseAsync();

        await using InterceptorTestDbContext seedContext = CreateContext();
        TestAuditableEntity entity = new()
        {
            Name = "Before update"
        };
        seedContext.AuditableEntities.Add(entity);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid entityId = entity.Id;
        Menlo.Lib.Common.ValueObjects.UserId? createdBy = entity.CreatedBy;
        DateTimeOffset? createdAt = entity.CreatedAt;
        DateTimeOffset? originalModifiedAt = entity.ModifiedAt;

        await using InterceptorTestDbContext updateContext = CreateContext();
        TestAuditableEntity entityToUpdate = await updateContext.AuditableEntities
            .SingleAsync(x => x.Id == entityId, TestContext.Current.CancellationToken);
        entityToUpdate.Name = "After update";
        await updateContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using InterceptorTestDbContext readContext = CreateContext();
        TestAuditableEntity? persistedEntity = await readContext.AuditableEntities
            .SingleOrDefaultAsync(x => x.Id == entityId, TestContext.Current.CancellationToken);

        ItShouldHavePersistedUpdatedEntity(persistedEntity);
        ItShouldHavePreservedCreatedAuditFields(persistedEntity, createdBy, createdAt);
        ItShouldHaveRefreshedModifiedAuditFields(persistedEntity, originalModifiedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_WithDeletedSoftDeletableEntity()
    {
        await ResetDatabaseAsync();

        await using InterceptorTestDbContext seedContext = CreateContext();
        TestSoftDeletableEntity entity = new()
        {
            Name = "Soft deletable"
        };
        seedContext.SoftDeletableEntities.Add(entity);
        await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Guid entityId = entity.Id;

        await using InterceptorTestDbContext deleteContext = CreateContext();
        TestSoftDeletableEntity entityToDelete = await deleteContext.SoftDeletableEntities
            .SingleAsync(x => x.Id == entityId, TestContext.Current.CancellationToken);
        deleteContext.SoftDeletableEntities.Remove(entityToDelete);
        await deleteContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using InterceptorTestDbContext readContext = CreateContext();
        TestSoftDeletableEntity? persistedEntity = await readContext.SoftDeletableEntities
            .SingleOrDefaultAsync(x => x.Id == entityId, TestContext.Current.CancellationToken);

        ItShouldHaveRetainedSoftDeletedRow(persistedEntity);
        ItShouldHaveMarkedEntityAsDeleted(persistedEntity);
        ItShouldHaveRecordedDeleteAuditFields(persistedEntity);
    }

    private InterceptorTestDbContext CreateContext()
    {
        return InterceptorTestDbContextFactory.Create(fixture.Services, fixture.ConnectionString);
    }

    private async Task ResetDatabaseAsync()
    {
        await using InterceptorTestDbContext context = CreateContext();
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE test_soft_deletable_entities, test_auditable_entities",
            TestContext.Current.CancellationToken);
    }

    private static void ItShouldHavePersistedAuditableEntity(TestAuditableEntity? persistedEntity)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.Name.ShouldBe("New entity");
    }

    private static void ItShouldHaveCreatedAuditFields(TestAuditableEntity? persistedEntity)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        persistedEntity.CreatedAt.ShouldNotBeNull();
    }

    private static void ItShouldHaveModifiedAuditFields(TestAuditableEntity? persistedEntity)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        persistedEntity.ModifiedAt.ShouldNotBeNull();
    }

    private static void ItShouldHavePersistedUpdatedEntity(TestAuditableEntity? persistedEntity)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.Name.ShouldBe("After update");
    }

    private static void ItShouldHavePreservedCreatedAuditFields(
        TestAuditableEntity? persistedEntity,
        Menlo.Lib.Common.ValueObjects.UserId? createdBy,
        DateTimeOffset? createdAt)
    {
        persistedEntity.ShouldNotBeNull();
        createdAt.ShouldNotBeNull();
        persistedEntity.CreatedBy.ShouldBe(createdBy);
        persistedEntity.CreatedAt.ShouldNotBeNull();
        persistedEntity.CreatedAt.Value.ShouldBe(createdAt.Value, TimeSpan.FromMilliseconds(1));
    }

    private static void ItShouldHaveRefreshedModifiedAuditFields(
        TestAuditableEntity? persistedEntity,
        DateTimeOffset? originalModifiedAt)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        originalModifiedAt.ShouldNotBeNull();
        persistedEntity.ModifiedAt.ShouldNotBeNull();
        persistedEntity.ModifiedAt.Value.ShouldBeGreaterThanOrEqualTo(originalModifiedAt.Value);
    }

    private static void ItShouldHaveRetainedSoftDeletedRow(TestSoftDeletableEntity? persistedEntity)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.Name.ShouldBe("Soft deletable");
    }

    private static void ItShouldHaveMarkedEntityAsDeleted(TestSoftDeletableEntity? persistedEntity)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.IsDeleted.ShouldBeTrue();
    }

    private static void ItShouldHaveRecordedDeleteAuditFields(TestSoftDeletableEntity? persistedEntity)
    {
        persistedEntity.ShouldNotBeNull();
        persistedEntity.DeletedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        persistedEntity.DeletedAt.ShouldNotBeNull();
    }
}
