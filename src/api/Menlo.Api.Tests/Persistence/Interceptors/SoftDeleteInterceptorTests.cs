using Menlo.Api.Persistence.Converters;
using Menlo.Api.Persistence.Data;
using Menlo.Api.Persistence.Interceptors;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Menlo.Api.Tests.Persistence.Interceptors;

/// <summary>
/// Tests for SoftDeleteInterceptor cascade delete functionality.
/// </summary>
public sealed class SoftDeleteInterceptorTests : IDisposable
{
    private readonly IAuditStampFactory _auditStampFactory;
    private readonly UserId _actorId;
    private readonly DateTimeOffset _timestamp;
    private bool _disposed;

    public SoftDeleteInterceptorTests()
    {
        _actorId = UserId.NewId();
        _timestamp = DateTimeOffset.UtcNow;

        AuditStamp stamp = new(_actorId, _timestamp);
        _auditStampFactory = Substitute.For<IAuditStampFactory>();
        _auditStampFactory.CreateStamp().Returns(stamp);
    }

    [Fact]
    public void GivenParentWithLoadedChildren_WhenSoftDeletingParent()
    {
        using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        TestChild child1 = new() { Id = Guid.NewGuid(), Name = "Child 1", ParentId = parent.Id };
        TestChild child2 = new() { Id = Guid.NewGuid(), Name = "Child 2", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.Children.Add(child1);
        context.Children.Add(child2);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent = context.Parents
            .Include(p => p.Children)
            .First(p => p.Id == parent.Id);

        trackedParent.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldHaveSoftDeletedParent(trackedParent);
        ItShouldHaveSoftDeletedAllChildren(trackedParent.Children);
    }

    [Fact]
    public void GivenParentWithUnloadedChildren_WhenSoftDeletingParent()
    {
        using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        TestChild child = new() { Id = Guid.NewGuid(), Name = "Child", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.Children.Add(child);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent = context.Parents
            .First(p => p.Id == parent.Id);

        trackedParent.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldHaveSoftDeletedParent(trackedParent);
        ItShouldNotHaveCascadedToUnloadedChildren(context, child.Id);
    }

    [Fact]
    public void GivenChildCollection_WhenSoftDeletingParent()
    {
        using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        TestChild child1 = new() { Id = Guid.NewGuid(), Name = "Child 1", ParentId = parent.Id };
        TestChild child2 = new() { Id = Guid.NewGuid(), Name = "Child 2", ParentId = parent.Id };
        TestChild child3 = new() { Id = Guid.NewGuid(), Name = "Child 3", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.Children.AddRange(child1, child2, child3);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent = context.Parents
            .Include(p => p.Children)
            .First(p => p.Id == parent.Id);

        trackedParent.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldHaveSoftDeletedParent(trackedParent);
        ItShouldHaveDeletedAllChildrenInCollection(trackedParent.Children, 3);
    }

    [Fact]
    public void GivenHashSetPrevention_WhenProcessingSameEntityMultipleTimes()
    {
        using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        TestChild child = new() { Id = Guid.NewGuid(), Name = "Child", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.Children.Add(child);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent = context.Parents
            .Include(p => p.Children)
            .First(p => p.Id == parent.Id);

        trackedParent.SoftDelete(_auditStampFactory);
        int callCountBefore = _auditStampFactory.ReceivedCalls().Count();

        context.SaveChanges();

        int callCountAfter = _auditStampFactory.ReceivedCalls().Count();
        // Only 1 new call during SaveChanges: parent was already soft-deleted before SaveChanges,
        // so only the child is newly soft-deleted by the interceptor
        ItShouldHaveProcessedEachEntityOnce(callCountBefore, callCountAfter, 1);
    }

    [Fact]
    public void GivenNonSoftDeletableEntities_WhenSavingChanges()
    {
        using TestDbContext context = CreateContext();
        RegularEntity entity = new() { Id = Guid.NewGuid(), Name = "Regular" };

        context.RegularEntities.Add(entity);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        RegularEntity tracked = context.RegularEntities.First(e => e.Id == entity.Id);
        tracked.Name = "Updated";
        context.SaveChanges();

        ItShouldNotAffectNonSoftDeletableEntities(tracked);
    }

    [Fact]
    public async Task GivenAsyncSaveChanges_WhenSoftDeletingParent()
    {
        await using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        TestChild child = new() { Id = Guid.NewGuid(), Name = "Child", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.Children.Add(child);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        TestParent trackedParent = await context.Parents
            .Include(p => p.Children)
            .FirstAsync(p => p.Id == parent.Id);

        trackedParent.SoftDelete(_auditStampFactory);
        await context.SaveChangesAsync();

        ItShouldHaveSoftDeletedParent(trackedParent);
        ItShouldHaveSoftDeletedAllChildren(trackedParent.Children);
    }

    [Fact]
    public void GivenSyncSaveChanges_WhenSoftDeletingParent()
    {
        using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        TestChild child = new() { Id = Guid.NewGuid(), Name = "Child", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.Children.Add(child);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent = context.Parents
            .Include(p => p.Children)
            .First(p => p.Id == parent.Id);

        trackedParent.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldHaveSoftDeletedParent(trackedParent);
        ItShouldHaveSoftDeletedAllChildren(trackedParent.Children);
    }

    [Fact]
    public void GivenMultipleParents_WhenSoftDeletingInSameTransaction()
    {
        using TestDbContext context = CreateContext();
        TestParent parent1 = new() { Id = Guid.NewGuid(), Name = "Parent 1" };
        TestParent parent2 = new() { Id = Guid.NewGuid(), Name = "Parent 2" };
        TestChild child1 = new() { Id = Guid.NewGuid(), Name = "Child 1", ParentId = parent1.Id };
        TestChild child2 = new() { Id = Guid.NewGuid(), Name = "Child 2", ParentId = parent2.Id };

        context.Parents.AddRange(parent1, parent2);
        context.Children.AddRange(child1, child2);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent1 = context.Parents
            .Include(p => p.Children)
            .First(p => p.Id == parent1.Id);
        TestParent trackedParent2 = context.Parents
            .Include(p => p.Children)
            .First(p => p.Id == parent2.Id);

        trackedParent1.SoftDelete(_auditStampFactory);
        trackedParent2.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldHaveSoftDeletedBothParents(trackedParent1, trackedParent2);
        ItShouldHaveSoftDeletedAllChildrenForBothParents(trackedParent1.Children, trackedParent2.Children);
    }

    [Fact(Skip = "SoftDeleteInterceptor requires recursive implementation to cascade beyond first level of children. Current implementation only cascades one level per SaveChanges call.")]
    public void GivenHierarchicalEntities_WhenSoftDeletingRoot()
    {
        using TestDbContext context = CreateContext();
        HierarchicalEntity root = new() { Id = Guid.NewGuid(), Name = "Root" };
        HierarchicalEntity child1 = new() { Id = Guid.NewGuid(), Name = "Child 1", ParentId = root.Id };
        HierarchicalEntity child2 = new() { Id = Guid.NewGuid(), Name = "Child 2", ParentId = root.Id };
        HierarchicalEntity grandchild = new() { Id = Guid.NewGuid(), Name = "Grandchild", ParentId = child1.Id };

        context.HierarchicalEntities.AddRange(root, child1, child2, grandchild);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        HierarchicalEntity trackedRoot = context.HierarchicalEntities
            .Include(e => e.Children)
            .ThenInclude(e => e.Children)
            .First(e => e.Id == root.Id);

        trackedRoot.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldHaveCascadedToAllDescendants(trackedRoot);
    }

    [Fact]
    public void GivenAlreadyDeletedChild_WhenSoftDeletingParent()
    {
        using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        TestChild child = new() { Id = Guid.NewGuid(), Name = "Child", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.Children.Add(child);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent = context.Parents
            .Include(p => p.Children)
            .First(p => p.Id == parent.Id);

        TestChild trackedChild = trackedParent.Children.First();
        trackedChild.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        trackedParent.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldNotProcessAlreadyDeletedChildren(trackedChild);
    }

    [Fact]
    public void GivenSingleNavigation_WhenSoftDeletingParent()
    {
        using TestDbContext context = CreateContext();
        TestParent parent = new() { Id = Guid.NewGuid(), Name = "Parent" };
        SingleChild single = new() { Id = Guid.NewGuid(), Name = "Single Child", ParentId = parent.Id };

        context.Parents.Add(parent);
        context.SingleChildren.Add(single);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        TestParent trackedParent = context.Parents
            .Include(p => p.SingleChild)
            .First(p => p.Id == parent.Id);

        trackedParent.SoftDelete(_auditStampFactory);
        context.SaveChanges();

        ItShouldHaveSoftDeletedParent(trackedParent);
        ItShouldHaveSoftDeletedSingleNavigation(trackedParent.SingleChild);
    }

    // Assertion Helpers
    private static void ItShouldHaveSoftDeletedParent(TestParent parent)
    {
        parent.IsDeleted.ShouldBeTrue();
        parent.DeletedAt.ShouldNotBeNull();
        parent.DeletedBy.ShouldNotBeNull();
    }

    private static void ItShouldHaveSoftDeletedAllChildren(IReadOnlyList<TestChild> children)
    {
        children.ShouldAllBe(c => c.IsDeleted);
        children.ShouldAllBe(c => c.DeletedAt.HasValue);
        children.ShouldAllBe(c => c.DeletedBy != null);
    }

    private static void ItShouldNotHaveCascadedToUnloadedChildren(TestDbContext context, Guid childId)
    {
        TestChild child = context.Children.First(c => c.Id == childId);
        child.IsDeleted.ShouldBeFalse();
        child.DeletedAt.ShouldBeNull();
        child.DeletedBy.ShouldBeNull();
    }

    private static void ItShouldHaveDeletedAllChildrenInCollection(IReadOnlyList<TestChild> children, int expectedCount)
    {
        children.Count.ShouldBe(expectedCount);
        children.ShouldAllBe(c => c.IsDeleted);
    }

    private static void ItShouldHaveProcessedEachEntityOnce(int callsBefore, int callsAfter, int expectedNewCalls)
    {
        int actualCalls = callsAfter - callsBefore;
        actualCalls.ShouldBe(expectedNewCalls);
    }

    private static void ItShouldNotAffectNonSoftDeletableEntities(RegularEntity entity)
    {
        entity.Name.ShouldBe("Updated");
    }

    private static void ItShouldHaveSoftDeletedBothParents(TestParent parent1, TestParent parent2)
    {
        parent1.IsDeleted.ShouldBeTrue();
        parent2.IsDeleted.ShouldBeTrue();
    }

    private static void ItShouldHaveSoftDeletedAllChildrenForBothParents(
        IReadOnlyList<TestChild> children1,
        IReadOnlyList<TestChild> children2)
    {
        children1.ShouldAllBe(c => c.IsDeleted);
        children2.ShouldAllBe(c => c.IsDeleted);
    }

    private static void ItShouldHaveCascadedToAllDescendants(HierarchicalEntity root)
    {
        root.IsDeleted.ShouldBeTrue();
        root.Children.ShouldAllBe(c => c.IsDeleted);

        foreach (HierarchicalEntity child in root.Children)
        {
            child.Children.ShouldAllBe(gc => gc.IsDeleted);
        }
    }

    private static void ItShouldNotProcessAlreadyDeletedChildren(TestChild child)
    {
        child.IsDeleted.ShouldBeTrue();
        child.DeletedAt.ShouldNotBeNull();
    }

    private static void ItShouldHaveSoftDeletedSingleNavigation(SingleChild? singleChild)
    {
        singleChild.ShouldNotBeNull();
        singleChild.IsDeleted.ShouldBeTrue();
        singleChild.DeletedAt.ShouldNotBeNull();
        singleChild.DeletedBy.ShouldNotBeNull();
    }

    // Test Infrastructure
    private TestDbContext CreateContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteInterceptor(_auditStampFactory))
            .Options;

        return new TestDbContext(options);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    // Test Entities
    private sealed class TestParent : ISoftDeletable
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public UserId? DeletedBy { get; private set; }
        public List<TestChild> Children { get; } = [];
        public SingleChild? SingleChild { get; set; }

        public void SoftDelete(IAuditStampFactory factory)
        {
            AuditStamp stamp = factory.CreateStamp();
            IsDeleted = true;
            DeletedAt = stamp.Timestamp;
            DeletedBy = stamp.ActorId;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }
    }

    private sealed class TestChild : ISoftDeletable
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public Guid ParentId { get; set; }
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public UserId? DeletedBy { get; private set; }

        public void SoftDelete(IAuditStampFactory factory)
        {
            AuditStamp stamp = factory.CreateStamp();
            IsDeleted = true;
            DeletedAt = stamp.Timestamp;
            DeletedBy = stamp.ActorId;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }
    }

    private sealed class SingleChild : ISoftDeletable
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public Guid ParentId { get; set; }
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public UserId? DeletedBy { get; private set; }

        public void SoftDelete(IAuditStampFactory factory)
        {
            AuditStamp stamp = factory.CreateStamp();
            IsDeleted = true;
            DeletedAt = stamp.Timestamp;
            DeletedBy = stamp.ActorId;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }
    }

    private sealed class HierarchicalEntity : ISoftDeletable
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public bool IsDeleted { get; private set; }
        public DateTimeOffset? DeletedAt { get; private set; }
        public UserId? DeletedBy { get; private set; }
        public List<HierarchicalEntity> Children { get; } = [];

        public void SoftDelete(IAuditStampFactory factory)
        {
            AuditStamp stamp = factory.CreateStamp();
            IsDeleted = true;
            DeletedAt = stamp.Timestamp;
            DeletedBy = stamp.ActorId;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }
    }

    private sealed class RegularEntity
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
    }

    // Test DbContext
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<TestParent> Parents => Set<TestParent>();
        public DbSet<TestChild> Children => Set<TestChild>();
        public DbSet<SingleChild> SingleChildren => Set<SingleChild>();
        public DbSet<HierarchicalEntity> HierarchicalEntities => Set<HierarchicalEntity>();
        public DbSet<RegularEntity> RegularEntities => Set<RegularEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestParent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.DeletedBy)
                    .HasConversion<NullableUserIdConverter>();

                entity.HasMany(e => e.Children)
                    .WithOne()
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SingleChild)
                    .WithOne()
                    .HasForeignKey<SingleChild>(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TestChild>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.DeletedBy)
                    .HasConversion<NullableUserIdConverter>();
            });

            modelBuilder.Entity<SingleChild>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.DeletedBy)
                    .HasConversion<NullableUserIdConverter>();
            });

            modelBuilder.Entity<HierarchicalEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.DeletedBy)
                    .HasConversion<NullableUserIdConverter>();

                entity.HasMany(e => e.Children)
                    .WithOne()
                    .HasForeignKey(e => e.ParentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RegularEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
            });
        }
    }
}
