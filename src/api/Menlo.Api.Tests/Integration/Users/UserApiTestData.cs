using Menlo.Application.Auth;
using Menlo.Application.Users;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.ValueObjects;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Api.Tests.Integration.Users;

internal static class UserApiTestData
{
    public static async Task<User> ProvisionUserAsync(
        IServiceProvider services,
        string externalId,
        string email,
        string displayName)
    {
        using IServiceScope scope = services.CreateScope();
        UserProvisioningService provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();

        await provisioningService.ProvisionOrUpdateAsync(externalId, email, displayName, TestContext.Current!.Execution.CancellationToken);

        return await FindByExternalIdAsync(services, externalId)
            ?? throw new InvalidOperationException($"User '{externalId}' was not provisioned.");
    }

    public static async Task<User?> FindByExternalIdAsync(IServiceProvider services, string externalId)
    {
        using IServiceScope scope = services.CreateScope();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        ExternalUserId normalizedExternalId = new(externalId);

        return await userContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);
    }

    public static async Task<int> CountByExternalIdAsync(IServiceProvider services, string externalId)
    {
        using IServiceScope scope = services.CreateScope();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
        ExternalUserId normalizedExternalId = new(externalId);

        return await userContext.Users
            .CountAsync(user => user.ExternalId == normalizedExternalId, TestContext.Current!.Execution.CancellationToken);
    }

    public static async Task<User> GetUserAsync(IServiceProvider services, UserId userId)
    {
        using IServiceScope scope = services.CreateScope();
        IUserContext userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();

        return await userContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == userId, TestContext.Current!.Execution.CancellationToken);
    }
}
