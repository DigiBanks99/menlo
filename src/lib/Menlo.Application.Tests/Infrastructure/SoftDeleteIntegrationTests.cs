using Menlo.Application.Auth;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Infrastructure;

[ClassDataSource<PersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Persistence")]
public sealed class SoftDeleteIntegrationTests(PersistenceFixture fixture)
{
    [Test]
    public async Task DeleteUserIntegrationTest()
    {
        // Arrange - create and persist a user
        User user = await CreateAndPersistUserAsync();

        // Act - delete via DbSet.Remove and save
        using (IServiceScope scope = fixture.Services.CreateScope())
        {
            IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
            userContext.Users.Remove(user);
            await userContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);
        }

        // Assert - default queries should not return the deleted user
        using (IServiceScope scope = fixture.Services.CreateScope())
        {
            IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
            List<User> all = await userContext.Users.ToListAsync(TestContext.Current!.Execution.CancellationToken);
            all.Any(u => u.Id == user.Id).ShouldBeFalse();
        }
    }

    [Test]
    public async Task DeletedUserIgnoredFilterTest()
    {
        // Arrange - create and persist a user
        User user = await CreateAndPersistUserAsync();

        // Act - delete via DbSet.Remove and save
        using (IServiceScope scope = fixture.Services.CreateScope())
        {
            IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
            userContext.Users.Remove(user);
            await userContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);
        }

        // Assert - using IgnoreQueryFilters we should see soft-delete metadata
        using (IServiceScope scope = fixture.Services.CreateScope())
        {
            IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
            User? deleted = await userContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == user.Id, TestContext.Current!.Execution.CancellationToken);

            deleted.ShouldNotBeNull();
            deleted.IsDeleted.ShouldBeTrue();
            deleted.DeletedAt.ShouldNotBeNull();
            deleted.DeletedBy.ShouldNotBeNull();
            deleted.DeletedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        }
    }

    private async Task<User> CreateAndPersistUserAsync()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        User user = User.Create(
                new ExternalUserId($"external-{Guid.NewGuid():N}"),
                $"test-{Guid.NewGuid():N}@menlo.app",
                "Soft Delete Test User")
            .Value;

        userContext.Users.Add(user);
        await userContext.SaveChangesAsync(TestContext.Current!.Execution.CancellationToken);

        return user;
    }
}
