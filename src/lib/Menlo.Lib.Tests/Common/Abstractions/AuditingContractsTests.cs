using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.Enums;
using Menlo.Lib.Common.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Common.Abstractions;

/// <summary>
/// Tests for Auditing contracts.
/// TC-05: Audit Create Operation
/// TC-06: Audit Update Operation
/// </summary>
public sealed class AuditingContractsTests
{
    private sealed class FakeAuditStampFactory(AuditStamp stamp) : IAuditStampFactory
    {
        private readonly AuditStamp _stamp = stamp;

        public AuditStamp CreateStamp() => _stamp;
    }

    private sealed class TestAuditableEntity : IAuditable
    {
        public UserId? CreatedBy { get; private set; }
        public DateTimeOffset? CreatedAt { get; private set; }
        public UserId? ModifiedBy { get; private set; }
        public DateTimeOffset? ModifiedAt { get; private set; }

        public void Audit(IAuditStampFactory factory, AuditOperation operation)
        {
            ArgumentNullException.ThrowIfNull(factory);

            AuditStamp stamp = factory.CreateStamp();

            switch (operation)
            {
                case AuditOperation.Create:
                    CreatedBy = stamp.ActorId;
                    CreatedAt = stamp.Timestamp;
                    ModifiedBy = stamp.ActorId;
                    ModifiedAt = stamp.Timestamp;
                    break;

                case AuditOperation.Update:
                    ModifiedBy = stamp.ActorId;
                    ModifiedAt = stamp.Timestamp;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown audit operation.");
            }
        }
    }

    [Fact]
    public void GivenEntityWithNullAuditFields_WhenAuditingWithCreate()
    {
        // Arrange
        TestAuditableEntity entity = new();
        UserId expectedUserId = UserId.NewId();
        DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow;
        AuditStamp stamp = new(expectedUserId, expectedTimestamp);
        FakeAuditStampFactory factory = new(stamp);

        ItShouldHaveNullAuditFields(entity);

        // Act
        entity.Audit(factory, AuditOperation.Create);

        // Assert
        ItShouldSetAllAuditFields(entity, expectedUserId, expectedTimestamp);
    }

    private static void ItShouldHaveNullAuditFields(TestAuditableEntity entity)
    {
        entity.CreatedBy.ShouldBeNull();
        entity.CreatedAt.ShouldBeNull();
        entity.ModifiedBy.ShouldBeNull();
        entity.ModifiedAt.ShouldBeNull();
    }

    private static void ItShouldSetAllAuditFields(TestAuditableEntity entity, UserId expectedUserId, DateTimeOffset expectedTimestamp)
    {
        entity.CreatedBy.ShouldBe(expectedUserId);
        entity.CreatedAt.ShouldBe(expectedTimestamp);
        entity.ModifiedBy.ShouldBe(expectedUserId);
        entity.ModifiedAt.ShouldBe(expectedTimestamp);
    }

    [Fact]
    public void GivenEntityWithExistingCreatedFields_WhenAuditingWithUpdate()
    {
        // Arrange
        TestAuditableEntity entity = new();
        UserId originalUserId = UserId.NewId();
        DateTimeOffset originalTimestamp = DateTimeOffset.UtcNow.AddDays(-1);
        AuditStamp createStamp = new(originalUserId, originalTimestamp);
        FakeAuditStampFactory createFactory = new(createStamp);

        // Simulate initial creation
        entity.Audit(createFactory, AuditOperation.Create);

        // Create new stamp for update
        UserId updateUserId = UserId.NewId();
        DateTimeOffset updateTimestamp = DateTimeOffset.UtcNow;
        AuditStamp updateStamp = new(updateUserId, updateTimestamp);
        FakeAuditStampFactory updateFactory = new(updateStamp);

        // Act
        entity.Audit(updateFactory, AuditOperation.Update);

        // Assert
        ItShouldKeepCreatedFieldsUnchanged(entity, originalUserId, originalTimestamp);
        ItShouldUpdateModifiedFields(entity, updateUserId, updateTimestamp);
    }

    private static void ItShouldKeepCreatedFieldsUnchanged(TestAuditableEntity entity, UserId originalUserId, DateTimeOffset originalTimestamp)
    {
        entity.CreatedBy.ShouldBe(originalUserId);
        entity.CreatedAt.ShouldBe(originalTimestamp);
    }

    private static void ItShouldUpdateModifiedFields(TestAuditableEntity entity, UserId updateUserId, DateTimeOffset updateTimestamp)
    {
        entity.ModifiedBy.ShouldBe(updateUserId);
        entity.ModifiedAt.ShouldBe(updateTimestamp);
    }

    [Fact]
    public void GivenNullFactory_WhenAuditing()
    {
        // Arrange
        TestAuditableEntity entity = new();

        // Act & Assert
        ItShouldThrowArgumentNullException(() => entity.Audit(null!, AuditOperation.Create));
    }

    private static void ItShouldThrowArgumentNullException(Action action)
    {
        Should.Throw<ArgumentNullException>(action);
    }
}


