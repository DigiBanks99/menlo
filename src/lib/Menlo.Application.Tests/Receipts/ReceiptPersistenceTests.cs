using Menlo.Application.Receipts;
using Menlo.Application.Tests.Fixtures;
using Menlo.Lib.Common;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Receipts.Entities;
using Menlo.Lib.Receipts.Enums;
using Menlo.Lib.Receipts.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Menlo.Application.Tests.Receipts;

[ClassDataSource<PersistenceFixture>(Shared = SharedType.PerTestSession)]
[NotInParallel("Persistence")]
public sealed class ReceiptPersistenceTests(PersistenceFixture fixture)
{
    private static readonly UserId Member = UserId.NewId();
    private static readonly DateTimeOffset SentAt = new(2026, 11, 28, 7, 30, 0, TimeSpan.Zero);

    private static readonly ReceiptReading Reading = new(
        Total: Money.Create(250.50m, "ZAR").Value,
        Date: new DateOnly(2026, 11, 27),
        Merchant: "The corner shop",
        IsRefund: false,
        Lines: [new ReceiptLine("Bread", Money.Create(20.50m, "ZAR").Value), new ReceiptLine(null, Money.Create(230m, "ZAR").Value)],
        UnreadLines: 1,
        SawMoreThanOneTotal: null);

    [Test]
    public async Task AReadReceiptIsSaved()
    {
        CancellationToken cancellationToken = TestContext.Current!.Execution.CancellationToken;
        Receipt receipt = Receipt.Send(2026, Member, SentAt, [[1, 2, 3], [4, 5]], "/budgets/2026").Value;
        receipt.StartReading();
        receipt.TimeOut();
        receipt.StartReading();
        receipt.Read(Reading);

        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            IReceiptContext receipts = scope.ServiceProvider.GetRequiredService<IReceiptContext>();
            receipts.Receipts.Add(receipt);
            await receipts.SaveChangesAsync(cancellationToken);
        }

        await using AsyncServiceScope readScope = fixture.Services.CreateAsyncScope();
        Receipt? saved = await readScope.ServiceProvider.GetRequiredService<IReceiptContext>().Receipts
            .Include(r => r.Photos)
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == receipt.Id, cancellationToken);

        ItShouldKeepTheReceipt();
        ItShouldKeepWhatWasRead();
        ItShouldKeepThePhotosInOrder();
        ItShouldStampWhoSavedIt();

        void ItShouldKeepTheReceipt() => saved.ShouldNotBeNull().ShouldSatisfyAllConditions(
            r => r.Year.ShouldBe(2026),
            r => r.TakenBy.ShouldBe(Member),
            r => r.TakenAt.ShouldBe(SentAt),
            r => r.BudgetView.ShouldBe("/budgets/2026"),
            r => r.Status.ShouldBe(ReceiptStatus.ReadyToCheck),
            r => r.Attempts.ShouldBe(1));

        void ItShouldKeepWhatWasRead()
        {
            ReceiptReading reading = saved!.Reading.ShouldNotBeNull();
            reading.ShouldSatisfyAllConditions(
                r => r.Total.ShouldBe(Reading.Total),
                r => r.Date.ShouldBe(Reading.Date),
                r => r.Merchant.ShouldBe(Reading.Merchant),
                r => r.IsRefund.ShouldBe(false),
                r => r.Lines.ShouldBe(Reading.Lines),
                r => r.UnreadLines.ShouldBe(1),
                r => r.SawMoreThanOneTotal.ShouldBeNull());
        }

        void ItShouldKeepThePhotosInOrder()
        {
            saved!.Photos.Select(p => p.Position).ShouldBe([1, 2]);
            saved.Photos.Select(p => p.Jpeg).ShouldBe([[1, 2, 3], [4, 5]]);
        }

        void ItShouldStampWhoSavedIt() => saved!.CreatedBy.ShouldBe(TestAuditStampFactory.TestUserId);
    }

    [Test]
    public async Task AReceiptIsDeleted()
    {
        CancellationToken cancellationToken = TestContext.Current!.Execution.CancellationToken;
        Receipt receipt = Receipt.Send(2026, Member, SentAt, [[1], [2], [3]], null).Value;

        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            IReceiptContext receipts = scope.ServiceProvider.GetRequiredService<IReceiptContext>();
            receipts.Receipts.Add(receipt);
            await receipts.SaveChangesAsync(cancellationToken);
        }

        await using (AsyncServiceScope scope = fixture.Services.CreateAsyncScope())
        {
            IReceiptContext receipts = scope.ServiceProvider.GetRequiredService<IReceiptContext>();
            Receipt saved = await receipts.Receipts.SingleAsync(r => r.Id == receipt.Id, cancellationToken);
            receipts.Receipts.Remove(saved);
            await receipts.SaveChangesAsync(cancellationToken);
        }

        await using AsyncServiceScope readScope = fixture.Services.CreateAsyncScope();
        IReceiptContext context = readScope.ServiceProvider.GetRequiredService<IReceiptContext>();
        bool receiptLeft = await context.Receipts.IgnoreQueryFilters().AnyAsync(r => r.Id == receipt.Id, cancellationToken);
        int photosLeft = await context.ReceiptPhotos.CountAsync(p => p.ReceiptId == receipt.Id, cancellationToken);

        ItShouldBeGone();
        ItShouldTakeItsPhotosWithIt();

        void ItShouldBeGone() => receiptLeft.ShouldBeFalse();

        void ItShouldTakeItsPhotosWithIt() => photosLeft.ShouldBe(0);
    }
}
