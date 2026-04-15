using Menlo.Application.Auth;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Infrastructure;

[Collection("Persistence")]
public sealed class UserContextIntegrationTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task SaveChangesAsync_WithNewUser()
    {
        User user = await CreateAndPersistUserAsync();

        User? persistedUser = await LoadUserAsync(user.Id);

        ItShouldHavePersistedUser(persistedUser, user);
        ItShouldHavePersistedAuditFields(persistedUser, user);
    }

    private async Task<User> CreateAndPersistUserAsync()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        User user = User.Create(
                new ExternalUserId($"external-{Guid.NewGuid():N}"),
                $"test-{Guid.NewGuid():N}@menlo.app",
                "Persistence Test User")
            .Value;

        userContext.Users.Add(user);
        await userContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        return user;
    }

    private async Task<User?> LoadUserAsync(Menlo.Lib.Common.ValueObjects.UserId userId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();

        return await userContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, TestContext.Current.CancellationToken);
    }

    private static void ItShouldHavePersistedUser(User? persistedUser, User user)
    {
        persistedUser.ShouldNotBeNull();
        persistedUser.Id.ShouldBe(user.Id);
        persistedUser.ExternalId.ShouldBe(user.ExternalId);
        persistedUser.Email.ShouldBe(user.Email);
        persistedUser.DisplayName.ShouldBe(user.DisplayName);
        persistedUser.LastLoginAt.ShouldNotBeNull();
        user.LastLoginAt.ShouldNotBeNull();
        persistedUser.LastLoginAt.Value.ShouldBe(user.LastLoginAt.Value, TimeSpan.FromMilliseconds(1));
    }

    private static void ItShouldHavePersistedAuditFields(User? persistedUser, User user)
    {
        persistedUser.ShouldNotBeNull();
        user.CreatedBy.ShouldNotBeNull();
        user.CreatedAt.ShouldNotBeNull();
        user.ModifiedBy.ShouldNotBeNull();
        user.ModifiedAt.ShouldNotBeNull();
        persistedUser.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        persistedUser.CreatedBy.ShouldBe(user.CreatedBy);
        persistedUser.CreatedAt.ShouldNotBeNull();
        persistedUser.CreatedAt.Value.ShouldBe(user.CreatedAt.Value, TimeSpan.FromMilliseconds(1));
        persistedUser.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId);
        persistedUser.ModifiedBy.ShouldBe(user.ModifiedBy);
        persistedUser.ModifiedAt.ShouldNotBeNull();
        persistedUser.ModifiedAt.Value.ShouldBe(user.ModifiedAt.Value, TimeSpan.FromMilliseconds(1));
    }
}


