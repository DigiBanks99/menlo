using Menlo.Application.Common;

namespace Menlo.Application.Tests.Common;

/// <summary>
/// Tests for MenloDbContext and MenloDbContextFactory.
/// </summary>
public sealed class MenloDbContextTests
{
    [Fact]
    public void GivenDesignTimeArgs_WhenCreateDbContext_ThenReturnsValidContext()
    {
        // Arrange
        MenloDbContextFactory factory = new();

        // Act
        MenloDbContext context = factory.CreateDbContext([]);

        // Assert
        ItShouldReturnNonNullContext(context);
    }

    private static void ItShouldReturnNonNullContext(MenloDbContext context)
    {
        context.ShouldNotBeNull();
    }
}
