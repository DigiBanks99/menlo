using Menlo.Application.Budget;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Budget;

[ClassDataSource<PersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Persistence")]
public sealed class RentalUnitContextIntegrationTests(PersistenceFixture fixture)
{
    private static CancellationToken Cancellation => TestContext.Current!.Execution.CancellationToken;

    [Test]
    public async Task ARentalUnitIsSaved()
    {
        RentalUnit unit = RentalUnit.Create("Garden flat").Value;

        await AddAsync(unit);
        RentalUnit? stored = await LoadAsync(unit.Id);

        ItShouldBeStoredWithItsName();
        ItShouldBeStampedByWhoeverSavedIt();

        void ItShouldBeStoredWithItsName() => stored.ShouldNotBeNull().ShouldSatisfyAllConditions(
            u => u.Id.ShouldBe(unit.Id),
            u => u.Name.ShouldBe("Garden flat"));

        void ItShouldBeStampedByWhoeverSavedIt() => stored!.ShouldSatisfyAllConditions(
            u => u.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId),
            u => u.CreatedAt.ShouldNotBeNull(),
            u => u.ModifiedBy.ShouldBe(TestAuditStampFactory.TestUserId),
            u => u.ModifiedAt.ShouldNotBeNull());
    }

    [Test]
    public async Task ASavedRentalUnitIsRenamed()
    {
        RentalUnit unit = RentalUnit.Create("Garden flat").Value;
        await AddAsync(unit);
        RentalUnit? added = await LoadAsync(unit.Id);

        await RenameAsync(unit.Id, "Cottage");
        RentalUnit? renamed = await LoadAsync(unit.Id);

        ItShouldStoreTheNewName();
        ItShouldKeepWhenItWasAdded();
        ItShouldRecordTheChange();

        void ItShouldStoreTheNewName() => renamed.ShouldNotBeNull().Name.ShouldBe("Cottage");

        void ItShouldKeepWhenItWasAdded() => renamed!.CreatedAt.ShouldBe(added!.CreatedAt);

        void ItShouldRecordTheChange() => renamed!.ModifiedAt.ShouldNotBeNull().ShouldBeGreaterThanOrEqualTo(added!.ModifiedAt!.Value);
    }

    private async Task AddAsync(RentalUnit unit)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext context = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        context.RentalUnits.Add(unit);
        await context.SaveChangesAsync(Cancellation);
    }

    private async Task RenameAsync(RentalUnitId id, string name)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext context = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        RentalUnit unit = await context.RentalUnits.SingleAsync(u => u.Id == id, Cancellation);
        unit.Rename(name);
        await context.SaveChangesAsync(Cancellation);
    }

    private async Task<RentalUnit?> LoadAsync(RentalUnitId id)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        IBudgetContext context = scope.ServiceProvider.GetRequiredService<IBudgetContext>();
        return await context.RentalUnits.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, Cancellation);
    }
}
