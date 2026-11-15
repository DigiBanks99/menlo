using CSharpFunctionalExtensions;
using Menlo.Application.Common;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Auth.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Api.Tests.Integration.Users;

[ClassDataSource<UserPersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Users")]
public sealed class UserProvisioningServiceTests(UserPersistenceFixture fixture)
{
    [Test]
    public async Task GivenNewExternalUser_WhenProvisioningCalled_UserIsCreatedAndReturned()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService service = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        User returned = (await service.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current!.Execution.CancellationToken)).Value;

        User? persisted = await FindAsync(externalId);
        ItShouldHaveProvisionedUser(persisted, email, displayName, new ExternalUserId(externalId));
        returned.Id.ShouldBe(persisted!.Id);
    }

    [Test]
    public async Task GivenExistingExternalUser_WhenProvisioningCalled_ExistingUserIsReturnedAndNoNewRecordIsCreated()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService service = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        User first = (await service.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current!.Execution.CancellationToken)).Value;
        User second = (await service.ProvisionOrUpdateAsync(externalId, $"updated-{email}", $"Updated {displayName}", TestContext.Current!.Execution.CancellationToken)).Value;

        int userCount = await CountAsync(externalId);
        ItShouldHaveReturnedTheSameSingleUser(first, second, userCount);
        ItShouldRetainOriginalUserDetails(second, email, displayName);
    }

    [Test]
    public async Task GivenTwoConcurrentFirstRequests_WhenProvisioningCalled_BothSucceedAndOneUserExists()
    {
        (string externalId, string email, string displayName) = CreateProvisioningInput();
        CancellationToken cancellationToken = TestContext.Current!.Execution.CancellationToken;

        Task<User> Provision()
        {
            IServiceScope scope = fixture.Services.CreateScope();
            UserProvisioningService service = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
            return service.ProvisionOrUpdateAsync(externalId, email, displayName, cancellationToken)
                .ContinueWith(task => { scope.Dispose(); return task.Result.Value; }, TaskScheduler.Default);
        }

        User[] results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(Provision, cancellationToken)));

        int userCount = await CountAsync(externalId);
        userCount.ShouldBe(1);
        results.Select(user => user.Id).Distinct().Count().ShouldBe(1);
    }

    [Test]
    public async Task GivenIdentityWithoutEmail_WhenProvisioningCalled_InvalidUserDataErrorIsReturned()
    {
        (string externalId, _, string displayName) = CreateProvisioningInput();

        using IServiceScope scope = fixture.Services.CreateScope();
        UserProvisioningService service = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        Result<User, AuthError> result = await service.ProvisionOrUpdateAsync(externalId, " ", displayName, TestContext.Current!.Execution.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<InvalidUserDataError>();
        (await CountAsync(externalId)).ShouldBe(0);
    }

    private async Task<User?> FindAsync(string externalId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId persistedExternalId = new(externalId);
        return await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.ExternalId == persistedExternalId, TestContext.Current!.Execution.CancellationToken);
    }

    private async Task<int> CountAsync(string externalId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        MenloDbContext db = scope.ServiceProvider.GetRequiredService<MenloDbContext>();
        ExternalUserId persistedExternalId = new(externalId);
        return await db.Users.CountAsync(user => user.ExternalId == persistedExternalId, TestContext.Current!.Execution.CancellationToken);
    }

    private static (string externalId, string email, string displayName) CreateProvisioningInput()
    {
        string uniqueSuffix = Guid.NewGuid().ToString("N");
        return ($"external-{uniqueSuffix}", $"user-{uniqueSuffix}@menlo.app", $"User {uniqueSuffix}");
    }

    private static void ItShouldHaveProvisionedUser(User? user, string expectedEmail, string expectedDisplayName, ExternalUserId expectedExternalId)
    {
        user.ShouldNotBeNull();
        user.ExternalId.ShouldBe(expectedExternalId);
        user.Email.ShouldBe(expectedEmail);
        user.DisplayName.ShouldBe(expectedDisplayName);
    }

    private static void ItShouldHaveReturnedTheSameSingleUser(User first, User second, int userCount)
    {
        userCount.ShouldBe(1);
        second.Id.ShouldBe(first.Id);
    }

    private static void ItShouldRetainOriginalUserDetails(User user, string expectedEmail, string expectedDisplayName)
    {
        user.Email.ShouldBe(expectedEmail);
        user.DisplayName.ShouldBe(expectedDisplayName);
    }
}
