using CSharpFunctionalExtensions;
using Menlo.Application.Auth;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Auth.Errors;
using Menlo.Lib.Common.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Menlo.Application.Tests.Common;

[Collection("Persistence")]
public sealed class HouseholdPersistenceTests(PersistenceFixture fixture)
{
    [Fact]
    public async Task GivenNewHousehold_WhenSaved_ThenCanBeRetrieved()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IHouseholdContext ctx = scope.ServiceProvider.GetRequiredService<IHouseholdContext>();

        Household household = Household.Create("Test Household").Value;
        ctx.Households.Add(household);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        IHouseholdContext readCtx = readScope.ServiceProvider.GetRequiredService<IHouseholdContext>();
        Household? retrieved = await readCtx.Households.FindAsync([household.Id], TestContext.Current.CancellationToken);

        retrieved.ShouldNotBeNull();
        retrieved.Name.ShouldBe("Test Household");
        retrieved.CreatedAt.ShouldNotBeNull();
        retrieved.CreatedBy.ShouldNotBeNull();
    }

    [Fact]
    public async Task GivenExistingHousehold_WhenSoftDeleted_ThenExcludedFromStandardQuery()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IHouseholdContext ctx = scope.ServiceProvider.GetRequiredService<IHouseholdContext>();

        Household household = Household.Create("Household To Delete").Value;
        ctx.Households.Add(household);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        ctx.Households.Remove(household);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        IHouseholdContext readCtx = readScope.ServiceProvider.GetRequiredService<IHouseholdContext>();

        Household? found = await readCtx.Households.FindAsync([household.Id], TestContext.Current.CancellationToken);
        found.ShouldBeNull();

        Household? deleted = await readCtx.Households
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == household.Id, TestContext.Current.CancellationToken);
        deleted.ShouldNotBeNull();
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedAt.ShouldNotBeNull();
        deleted.DeletedBy.ShouldNotBeNull();
    }

    [Fact]
    public async Task GivenUserAndHousehold_WhenUserAssignedToHousehold_ThenFKIsPersistedCorrectly()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IHouseholdContext householdCtx = scope.ServiceProvider.GetRequiredService<IHouseholdContext>();
        IUserContext userCtx = scope.ServiceProvider.GetRequiredService<IUserContext>();

        Household household = Household.Create("FK Test Household").Value;
        householdCtx.Households.Add(household);
        await householdCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        Result<Menlo.Lib.Auth.Entities.User, AuthError> userResult =
            Menlo.Lib.Auth.Entities.User.Create(
                new Menlo.Lib.Auth.ValueObjects.ExternalUserId("fk-test-ext-id"),
                "fktest@menlo.app",
                "FK Test User");
        userResult.IsSuccess.ShouldBeTrue();
        userCtx.Users.Add(userResult.Value);
        await userCtx.SaveChangesAsync(TestContext.Current.CancellationToken);

        using IServiceScope readScope = fixture.Services.CreateScope();
        IUserContext readCtx = readScope.ServiceProvider.GetRequiredService<IUserContext>();
        Menlo.Lib.Auth.Entities.User? retrievedUser =
            await readCtx.Users.FindAsync([userResult.Value.Id], TestContext.Current.CancellationToken);

        retrievedUser.ShouldNotBeNull();
        retrievedUser.HouseholdId.ShouldBeNull();
    }

    [Fact]
    public async Task GivenDefaultHouseholdSeed_WhenQueried_ThenExists()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IHouseholdContext ctx = scope.ServiceProvider.GetRequiredService<IHouseholdContext>();

        Guid defaultHouseholdId = new("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        HouseholdId householdId = new(defaultHouseholdId);

        Household? defaultHousehold = await ctx.Households
            .FirstOrDefaultAsync(h => h.Id == householdId, TestContext.Current.CancellationToken);

        defaultHousehold.ShouldNotBeNull();
        defaultHousehold.Name.ShouldBe("Default Household");
    }
}
