using Menlo.Api.Persistence.Data;
using Menlo.Api.Persistence.Interceptors;
using Menlo.Lib.Common.Abstractions;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Menlo.Api.Tests.Persistence.Fixtures;

/// <summary>
/// Test fixture providing an in-memory EF Core database for testing.
/// Includes auditing and soft delete interceptors with predictable mock data.
/// </summary>
public sealed class DbContextFixture : IDisposable
{
    private readonly MenloDbContext _context;
    private readonly IAuditStampFactory _mockAuditStampFactory;
    private bool _disposed;

    /// <summary>
    /// Predictable User ID for testing (all zeros).
    /// </summary>
    public static readonly UserId TestUserId = new(Guid.Empty);

    /// <summary>
    /// Predictable User ID for testing (all ones).
    /// </summary>
    public static readonly UserId AlternateTestUserId = new(new Guid("11111111-1111-1111-1111-111111111111"));

    /// <summary>
    /// Predictable timestamp for testing.
    /// </summary>
    public static readonly DateTimeOffset TestTimestamp = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Predictable correlation ID for testing.
    /// </summary>
    public const string TestCorrelationId = "test-correlation-id";

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextFixture"/> class.
    /// </summary>
    public DbContextFixture()
    {
        // Create mock audit stamp factory with predictable values
        _mockAuditStampFactory = Substitute.For<IAuditStampFactory>();
        _mockAuditStampFactory.CreateStamp().Returns(new AuditStamp(
            TestUserId,
            TestTimestamp,
            TestCorrelationId));

        // Create in-memory database options with interceptors
        DbContextOptions<MenloDbContext> options = new DbContextOptionsBuilder<MenloDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per fixture
            .AddInterceptors(
                new AuditingInterceptor(_mockAuditStampFactory),
                new SoftDeleteInterceptor(_mockAuditStampFactory))
            .EnableSensitiveDataLogging() // Helpful for debugging tests
            .Options;

        _context = new MenloDbContext(options);
    }

    /// <summary>
    /// Gets the MenloDbContext instance for testing.
    /// </summary>
    public MenloDbContext Context => _context;

    /// <summary>
    /// Gets the mock audit stamp factory for configuring test scenarios.
    /// </summary>
    public IAuditStampFactory MockAuditStampFactory => _mockAuditStampFactory;

    /// <summary>
    /// Configures the mock audit stamp factory to return a specific stamp.
    /// </summary>
    /// <param name="actorId">The actor ID for the stamp.</param>
    /// <param name="timestamp">The timestamp for the stamp.</param>
    /// <param name="correlationId">The optional correlation ID.</param>
    public void SetAuditStamp(UserId actorId, DateTimeOffset timestamp, string? correlationId = null)
    {
        _mockAuditStampFactory.CreateStamp().Returns(new AuditStamp(actorId, timestamp, correlationId));
    }

    /// <summary>
    /// Resets the audit stamp factory to return the default test values.
    /// </summary>
    public void ResetAuditStamp()
    {
        _mockAuditStampFactory.CreateStamp().Returns(new AuditStamp(
            TestUserId,
            TestTimestamp,
            TestCorrelationId));
    }

    /// <summary>
    /// Detaches all tracked entities to ensure a clean state for the next test.
    /// Useful when you need to simulate a fresh DbContext within the same fixture instance.
    /// </summary>
    public void DetachAllEntities()
    {
        foreach (var entry in _context.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context.Dispose();
        _disposed = true;
    }
}
