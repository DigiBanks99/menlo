using CSharpFunctionalExtensions;
using Menlo.Lib.Common.ValueObjects;
using Menlo.Lib.Receipts.Entities;
using Menlo.Lib.Receipts.Enums;
using Menlo.Lib.Receipts.Errors;
using Menlo.Lib.Receipts.ValueObjects;
using Shouldly;

namespace Menlo.Lib.Tests.Receipts.Entities;

public sealed class ReceiptTests
{
    private static readonly UserId Member = UserId.NewId();
    private static readonly DateTimeOffset SentAt = new(2026, 11, 28, 9, 30, 0, TimeSpan.FromHours(2));

    private static readonly ReceiptReading UsableReading = ReceiptReading.Empty with
    {
        Total = Money.Create(123.45m, "ZAR").Value,
        Merchant = "The corner shop"
    };

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    public void AMemberSendsPhotosOfASlip(int photoCount)
    {
        byte[][] photos = [.. Enumerable.Range(1, photoCount).Select(n => new byte[] { (byte)n })];

        Result<Receipt, ReceiptError> result = Receipt.Send(2026, Member, SentAt, photos, "/budgets/2026?month=11");

        ItShouldBeSent();
        ItShouldBeWaiting();
        ItShouldKnowWhoTookItAndWhen();
        ItShouldKeepThePhotosInOrder();
        ItShouldHaveNothingReadYet();

        void ItShouldBeSent() => result.IsSuccess.ShouldBeTrue();

        void ItShouldBeWaiting() => result.Value.Status.ShouldBe(ReceiptStatus.Waiting);

        void ItShouldKnowWhoTookItAndWhen() => result.Value.ShouldSatisfyAllConditions(
            receipt => receipt.Year.ShouldBe(2026),
            receipt => receipt.TakenBy.ShouldBe(Member),
            receipt => receipt.TakenAt.ShouldBe(SentAt),
            receipt => receipt.BudgetView.ShouldBe("/budgets/2026?month=11"));

        void ItShouldKeepThePhotosInOrder()
        {
            result.Value.Photos.Select(photo => photo.Position).ShouldBe(Enumerable.Range(1, photoCount));
            result.Value.Photos.Select(photo => photo.Jpeg[0]).ShouldBe(Enumerable.Range(1, photoCount).Select(n => (byte)n));
            result.Value.Photos.ShouldAllBe(photo => photo.ReceiptId == result.Value.Id);
        }

        void ItShouldHaveNothingReadYet() => result.Value.ShouldSatisfyAllConditions(
            receipt => receipt.Reading.ShouldBeNull(),
            receipt => receipt.Attempts.ShouldBe(0));
    }

    [Test]
    [Arguments(0)]
    [Arguments(4)]
    public void AMemberSendsTheWrongNumberOfPhotos(int photoCount)
    {
        byte[][] photos = [.. Enumerable.Range(1, photoCount).Select(_ => new byte[] { 1 })];

        Result<Receipt, ReceiptError> result = Receipt.Send(2026, Member, SentAt, photos, null);

        ItShouldBeRefused();

        void ItShouldBeRefused() => result.Error
            .ShouldBeOfType<InvalidPhotoCountError>()
            .Count.ShouldBe(photoCount);
    }

    [Test]
    public void AReceiptIsSentForAYearThatCantBeABudgetYear()
    {
        Result<Receipt, ReceiptError> result = Receipt.Send(0, Member, SentAt, [[1]], null);

        ItShouldBeRefused();

        void ItShouldBeRefused() => result.Error.ShouldBeOfType<InvalidReceiptYearError>();
    }

    [Test]
    public void AReceiptIsSentFromNoBudgetView()
    {
        Receipt receipt = Receipt.Send(2026, Member, SentAt, [[1]], "  ").Value;

        ItShouldHaveNoViewToGoBackTo();

        void ItShouldHaveNoViewToGoBackTo() => receipt.BudgetView.ShouldBeNull();
    }

    [Test]
    public void TheAgentReadsSomethingUsable()
    {
        Receipt receipt = AReceiptBeingRead();

        UnitResult<ReceiptError> result = receipt.Read(UsableReading);

        ItShouldSucceed(result);
        ItShouldBeReadyToCheck();
        ItShouldKeepWhatWasRead();

        void ItShouldBeReadyToCheck() => receipt.Status.ShouldBe(ReceiptStatus.ReadyToCheck);

        void ItShouldKeepWhatWasRead() => receipt.Reading.ShouldBe(UsableReading);
    }

    [Test]
    public void TheAgentReadsNothingUsable()
    {
        Receipt receipt = AReceiptBeingRead();
        ReceiptReading nothing = ReceiptReading.Empty with { UnreadLines = 4, Lines = [new ReceiptLine(" ", null)] };

        UnitResult<ReceiptError> result = receipt.Read(nothing);

        ItShouldSucceed(result);
        ItShouldNotHaveBeenRead();

        void ItShouldNotHaveBeenRead() => receipt.Status.ShouldBe(ReceiptStatus.CouldntRead);
    }

    [Test]
    public void TheModelIsDownWhileReading()
    {
        Receipt receipt = AReceiptBeingRead();

        UnitResult<ReceiptError> result = receipt.ReturnToWaiting();

        ItShouldSucceed(result);
        ItShouldWaitToBeReadAgain();
        ItShouldNotCountAsAnAttempt();

        void ItShouldWaitToBeReadAgain() => receipt.Status.ShouldBe(ReceiptStatus.Waiting);

        void ItShouldNotCountAsAnAttempt() => receipt.Attempts.ShouldBe(0);
    }

    [Test]
    public void TheFirstReadRunsOutOfTime()
    {
        Receipt receipt = AReceiptBeingRead();

        UnitResult<ReceiptError> result = receipt.TimeOut();

        ItShouldSucceed(result);
        ItShouldWaitToBeReadAgain();
        ItShouldCountTheAttempt();

        void ItShouldWaitToBeReadAgain() => receipt.Status.ShouldBe(ReceiptStatus.Waiting);

        void ItShouldCountTheAttempt() => receipt.Attempts.ShouldBe(1);
    }

    [Test]
    public void TheSecondReadRunsOutOfTime()
    {
        Receipt receipt = AReceiptBeingRead();
        receipt.TimeOut();
        receipt.StartReading();

        UnitResult<ReceiptError> result = receipt.TimeOut();

        ItShouldSucceed(result);
        ItShouldNotHaveBeenRead();
        ItShouldCountBothAttempts();

        void ItShouldNotHaveBeenRead() => receipt.Status.ShouldBe(ReceiptStatus.CouldntRead);

        void ItShouldCountBothAttempts() => receipt.Attempts.ShouldBe(2);
    }

    [Test]
    public void AReceiptAlreadyBeingReadIsStartedAgain()
    {
        Receipt receipt = AReceiptBeingRead();

        UnitResult<ReceiptError> result = receipt.StartReading();

        ItShouldBeRefused(result);
        ItShouldStillBeReading();

        void ItShouldStillBeReading() => receipt.Status.ShouldBe(ReceiptStatus.Reading);
    }

    [Test]
    public void AWaitingReceiptIsFinishedWithoutBeingRead()
    {
        Receipt receipt = AWaitingReceipt();

        UnitResult<ReceiptError>[] results =
        [
            receipt.Read(UsableReading),
            receipt.ReturnToWaiting(),
            receipt.TimeOut()
        ];

        ItShouldRefuseEveryChange();
        ItShouldStillBeWaiting();

        void ItShouldRefuseEveryChange() => results.ShouldAllBe(result => result.IsFailure && result.Error is InvalidReceiptStatusError);

        void ItShouldStillBeWaiting() => receipt.ShouldSatisfyAllConditions(
            r => r.Status.ShouldBe(ReceiptStatus.Waiting),
            r => r.Reading.ShouldBeNull(),
            r => r.Attempts.ShouldBe(0));
    }

    [Test]
    public void AReceiptReadyToCheckIsStartedAgain()
    {
        Receipt receipt = AReceiptBeingRead();
        receipt.Read(UsableReading);

        UnitResult<ReceiptError> result = receipt.StartReading();

        ItShouldBeRefused(result);
    }

    private static Receipt AWaitingReceipt() => Receipt.Send(2026, Member, SentAt, [[1]], null).Value;

    private static Receipt AReceiptBeingRead()
    {
        Receipt receipt = AWaitingReceipt();
        receipt.StartReading().IsSuccess.ShouldBeTrue();
        return receipt;
    }

    private static void ItShouldSucceed(UnitResult<ReceiptError> result) => result.IsSuccess.ShouldBeTrue();

    private static void ItShouldBeRefused(UnitResult<ReceiptError> result) =>
        result.Error.ShouldBeOfType<InvalidReceiptStatusError>();
}
