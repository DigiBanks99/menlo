using System.Globalization;

namespace Menlo.Ui.Tests.Charts;

public sealed class ChartTests
{
    [Test]
    [Arguments(0, new[] { 0d, 1d })]
    [Arguments(4, new[] { 0d, 1d, 2d, 3d, 4d })]
    [Arguments(3200, new[] { 0d, 1000d, 2000d, 3000d, 4000d })]
    [Arguments(11350, new[] { 0d, 5000d, 10000d, 15000d })]
    public void GivenAMaximum_WhenTicksAreWorkedOut_ThenTheyAreRoundAndReachIt(double max, double[] expected)
    {
        IReadOnlyList<decimal> ticks = ChartMath.Ticks((decimal)max);

        ticks.Select(tick => (double)tick).ShouldBe(expected);
    }

    [Test]
    public void GivenValues_WhenSharesAreWorkedOut_ThenTheyAddUpToOneAndIgnoreNegatives()
    {
        IReadOnlyList<double> shares = ChartMath.Shares([30m, -10m, 10m]);

        shares.ShouldBe(new[] { 0.75d, 0d, 0.25d });
    }

    [Test]
    public void GivenNothingToShare_WhenSharesAreWorkedOut_ThenEveryShareIsZero()
    {
        IReadOnlyList<double> shares = ChartMath.Shares([0m, 0m]);

        shares.ShouldBe(new[] { 0d, 0d });
    }

    [Test]
    public async Task GivenTwoSeriesOverThreeCategories_WhenTheBarChartIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlBarChart>(new Dictionary<string, object?>
        {
            ["Title"] = "Planned and spent",
            ["Categories"] = new[] { "Groceries", "Utilities", "Transport" },
            ["Series"] = new MnlChartSeries[]
            {
                new("Planned", [4000m, 2000m, 1000m]),
                new("Spent", [2000m, 1000m, 0m])
            }
        });

        IHtmlCollection<IElement> bars = document.QuerySelectorAll("rect.mnl-chart__bar");

        ItShouldBeNamedForScreenReaders();
        ItShouldDrawOneBarPerValue();
        ItShouldScaleBarsToTheirValues();
        ItShouldExplainEachBarOnHover();
        ItShouldHaveALegendForTheSeries();

        void ItShouldBeNamedForScreenReaders()
        {
            IElement svg = document.Find("svg");
            svg.GetAttribute("role").ShouldBe("img");
            svg.GetAttribute("aria-label").ShouldBe("Planned and spent");
        }

        void ItShouldDrawOneBarPerValue() => bars.Length.ShouldBe(6);

        void ItShouldScaleBarsToTheirValues()
        {
            double Height(IElement bar) => double.Parse(bar.GetAttribute("height")!, CultureInfo.InvariantCulture);

            // 4000 is the top of the axis: Planned Groceries is twice Spent Groceries, and zero has no height
            Height(bars[0]).ShouldBe(Height(bars[1]) * 2, tolerance: 0.5);
            Height(bars[5]).ShouldBe(0);
        }

        void ItShouldExplainEachBarOnHover() =>
            bars[0].Find("title").TextContent.ShouldBe("Planned, Groceries: 4,000");

        void ItShouldHaveALegendForTheSeries() =>
            document.QuerySelectorAll(".mnl-chart__legend-item").Select(item => item.TextContent.Trim()).ShouldBe(["Planned", "Spent"]);
    }

    [Test]
    public async Task GivenOneSeries_WhenTheLineChartIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlLineChart>(new Dictionary<string, object?>
        {
            ["Title"] = "Monthly spending",
            ["Categories"] = new[] { "Jan", "Feb", "Mar" },
            ["Series"] = new MnlChartSeries[] { new("This year", [100m, 300m, 200m]) }
        });

        ItShouldDrawOneLineThroughEveryPoint();
        ItShouldLabelTheAxisInSvgText();
        ItShouldNotNeedALegend();

        void ItShouldDrawOneLineThroughEveryPoint()
        {
            document.Find("polyline").GetAttribute("points")!.Split(' ').Length.ShouldBe(3);
            document.QuerySelectorAll("circle.mnl-chart__point").Length.ShouldBe(3);
        }

        void ItShouldLabelTheAxisInSvgText() =>
            document.QuerySelectorAll("text").Select(text => text.TextContent).ShouldContain("Feb");

        void ItShouldNotNeedALegend() => document.QuerySelector(".mnl-chart__legend").ShouldBeNull();
    }

    [Test]
    public async Task GivenSlices_WhenTheDonutChartIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlDonutChart>(new Dictionary<string, object?>
        {
            ["Title"] = "Spending by category",
            ["Slices"] = new MnlChartSlice[] { new("Groceries", 3m), new("Utilities", 1m) },
            ["CenterValue"] = "R 4"
        });

        IHtmlCollection<IElement> arcs = document.QuerySelectorAll("circle.mnl-chart__arc");

        ItShouldDrawAnArcPerSliceSizedByItsShare();
        ItShouldShowTheShareOnHover();
        ItShouldShowTheMiddleFigure();

        void ItShouldDrawAnArcPerSliceSizedByItsShare()
        {
            double Length(IElement arc) =>
                double.Parse(arc.GetAttribute("stroke-dasharray")!.Split(' ')[0], CultureInfo.InvariantCulture);

            arcs.Length.ShouldBe(2);
            Length(arcs[0]).ShouldBe(Length(arcs[1]) * 3, tolerance: 0.5);
        }

        void ItShouldShowTheShareOnHover() => arcs[0].Find("title").TextContent.ShouldBe("Groceries: 3 (75%)");

        void ItShouldShowTheMiddleFigure() => document.Find(".mnl-chart__center-value").TextContent.ShouldBe("R 4");
    }

    [Test]
    public async Task GivenMoreThanAHundredPercent_WhenTheRadialChartIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlRadialChart>(new Dictionary<string, object?>
        {
            ["Title"] = "Budget used",
            ["Value"] = 140m,
            ["Tone"] = MnlTone.Error
        });

        ItShouldCapTheFigureAtAHundred();

        void ItShouldCapTheFigureAtAHundred()
        {
            document.Find(".mnl-chart__center-value").TextContent.ShouldBe("100%");
            document.Find("svg").GetAttribute("aria-label").ShouldBe("Budget used: 100%");
            document.Find("figure").GetAttribute("data-tone").ShouldBe("error");
        }
    }
}
