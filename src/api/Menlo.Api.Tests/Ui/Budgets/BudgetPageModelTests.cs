using Menlo.Api.Ui.Budgets;
using Microsoft.AspNetCore.Http;

namespace Menlo.Api.Tests.Ui.Budgets;

/// <summary>
/// How the budget page reads its URL and turns a year and the current month into what it shows.
/// </summary>
public sealed class BudgetPageModelTests
{
    private static readonly DateOnly July2026 = new(2026, 7, 1);

    [Test]
    public void TheCurrentYearIsShownInJuly()
    {
        BudgetPageModel model = BudgetPageModel.Create(new BudgetQuery(2026), exists: true, [2025, 2026], July2026);

        ItShouldFreezeJanuaryToJune();
        ItShouldMarkJulyAsTheCurrentMonth();
        ItShouldLeaveAugustToDecemberAhead();
        ItShouldEndWithTheYear();

        void ItShouldFreezeJanuaryToJune() => model.Months.Take(6).ShouldAllBe(m => m.State == "frozen" && m.StateLabel == "Frozen");

        void ItShouldMarkJulyAsTheCurrentMonth() => model.Months[6].ShouldSatisfyAllConditions(
            m => m.Name.ShouldBe("Jul"),
            m => m.FullName.ShouldBe("July 2026"),
            m => m.State.ShouldBe("current"),
            m => m.StateLabel.ShouldBe("This month"),
            m => m.AriaCurrent.ShouldBe("date"));

        void ItShouldLeaveAugustToDecemberAhead() => model.Months.Skip(7).ShouldAllBe(m => m.State == "ahead" && m.StateLabel == null && m.AriaCurrent == null);

        void ItShouldEndWithTheYear() => model.ShouldSatisfyAllConditions(
            m => m.Months.Count.ShouldBe(12),
            m => m.YearTile.Name.ShouldBe("Year"));
    }

    [Test]
    [Arguments(2025, 12, 0)]
    [Arguments(2027, 0, 0)]
    public void AYearOtherThanTheCurrentOneIsShown(int year, int frozen, int current)
    {
        BudgetPageModel model = BudgetPageModel.Create(new BudgetQuery(year), exists: true, [year], July2026);

        ItShouldFreezeOnlyMonthsBeforeTheCurrentMonth();
        ItShouldMarkNoCurrentMonth();

        void ItShouldFreezeOnlyMonthsBeforeTheCurrentMonth() => model.Months.Count(m => m.State == "frozen").ShouldBe(frozen);

        void ItShouldMarkNoCurrentMonth() => model.Months.Count(m => m.IsCurrent).ShouldBe(current);
    }

    [Test]
    public void AYearWithNoBudgetIsShown()
    {
        BudgetPageModel model = BudgetPageModel.Create(new BudgetQuery(2027), exists: false, [2025, 2026], July2026);

        ItShouldOfferToCreateIt();
        ItShouldListItInThePickerAsHavingNoBudget();

        void ItShouldOfferToCreateIt() => model.Offer.ShouldSatisfyAllConditions(
            o => o.Action.ShouldBe("/budgets"),
            o => o.Year.ShouldBe("2027"),
            o => o.Label.ShouldBe("Create the 2027 budget"));

        void ItShouldListItInThePickerAsHavingNoBudget() => model.ShouldSatisfyAllConditions(
            m => m.ShowYearPicker.ShouldBeTrue(),
            m => m.Years.Select(o => o.Label).ShouldBe(["2025", "2026", "2027 (no budget yet)"]),
            m => m.SelectedYear.ShouldBe("2027"));
    }

    [Test]
    public void NoYearHasABudget()
    {
        BudgetPageModel model = BudgetPageModel.Create(new BudgetQuery(2026), exists: false, [], July2026);

        ItShouldHideTheYearPicker();

        void ItShouldHideTheYearPicker() => model.ShowYearPicker.ShouldBeFalse();
    }

    [Test]
    public void ASideRailSectionIsOpen()
    {
        BudgetPageModel model = BudgetPageModel.Create(new BudgetQuery(2026, "triggers"), exists: true, [2026], July2026);

        ItShouldShowTheSection();
        ItShouldMarkItsIconAsExpanded();
        ItShouldLinkItsIconToClosingIt();
        ItShouldLinkTheOtherIconsToOpeningThem();

        void ItShouldShowTheSection() => model.ShouldSatisfyAllConditions(
            m => m.OpenSection.ShouldNotBeNull().Section.Label.ShouldBe("Triggers"),
            m => m.RailState.ShouldBe("open"),
            m => m.CloseUrl.ShouldBe("/budgets?year=2026"));

        void ItShouldMarkItsIconAsExpanded() => model.OpenSection!.ShouldSatisfyAllConditions(
            i => i.Expanded.ShouldBe("true"),
            i => i.Controls.ShouldBe(BudgetRail.PanelId));

        void ItShouldLinkItsIconToClosingIt() => model.OpenSection!.ToggleUrl.ShouldBe("/budgets?year=2026");

        void ItShouldLinkTheOtherIconsToOpeningThem() => model.Rail
            .Where(i => !i.IsOpen)
            .ShouldAllBe(i => i.ToggleUrl == $"/budgets?year=2026&panel={i.Section.Key}" && i.Expanded == "false" && i.Controls == null);
    }

    [Test]
    public void TheSideRailIsClosed()
    {
        BudgetPageModel model = BudgetPageModel.Create(new BudgetQuery(2026), exists: true, [2026], July2026);

        ItShouldListEverySectionInOrder();
        ItShouldShowNoSection();

        void ItShouldListEverySectionInOrder() => model.Rail.Select(i => i.Section.Label).ShouldBe(
            ["Proposals", "Triggers", "Set-up checklist", "Year tools", "Who it is for", "How to read the grid"]);

        void ItShouldShowNoSection() => model.ShouldSatisfyAllConditions(
            m => m.OpenSection.ShouldBeNull(),
            m => m.RailState.ShouldBe("closed"));
    }

    [Test]
    [Arguments("?year=2031&panel=key", 2031, "key")]
    [Arguments("", 2026, null)]
    [Arguments("?year=next&panel=nothing", 2026, null)]
    [Arguments("?year=0", 2026, null)]
    [Arguments("?year=10000", 2026, null)]
    [Arguments("?year=-5", 2026, null)]
    public void TheAddressIsRead(string queryString, int year, string? panel)
    {
        DefaultHttpContext context = new();
        context.Request.QueryString = new QueryString(queryString);

        BudgetQuery query = BudgetQuery.From(context.Request, currentYear: 2026);

        ItShouldFallBackToTheCurrentYearAndAClosedRail();

        void ItShouldFallBackToTheCurrentYearAndAClosedRail() => query.ShouldBe(new BudgetQuery(year, panel));
    }
}
