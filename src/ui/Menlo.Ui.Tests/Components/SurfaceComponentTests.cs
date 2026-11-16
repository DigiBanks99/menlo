namespace Menlo.Ui.Tests.Components;

public sealed class SurfaceComponentTests
{
    [Test]
    public async Task GivenOnlyABody_WhenTheCardIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlCard>(new Dictionary<string, object?>
        {
            ["ChildContent"] = Rendered.Text("Body")
        });

        ItShouldHaveNoHeaderOrFooter();

        void ItShouldHaveNoHeaderOrFooter()
        {
            document.Find(".mnl-card__body").TextContent.ShouldBe("Body");
            document.QuerySelector(".mnl-card__header").ShouldBeNull();
            document.QuerySelector(".mnl-card__footer").ShouldBeNull();
        }
    }

    [Test]
    public async Task GivenAHeaderAndFooter_WhenAnInteractiveCardIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlCard>(new Dictionary<string, object?>
        {
            ["Header"] = Rendered.Text("Title"),
            ["Footer"] = Rendered.Text("Actions"),
            ["Interactive"] = true,
            ["Padding"] = MnlSize.Lg
        });

        ItShouldRenderAllThreeSections();
        ItShouldSayItIsInteractiveAndLarge();

        void ItShouldRenderAllThreeSections()
        {
            document.Find("header.mnl-card__header").TextContent.ShouldBe("Title");
            document.Find("footer.mnl-card__footer").TextContent.ShouldBe("Actions");
        }

        void ItShouldSayItIsInteractiveAndLarge()
        {
            IElement card = document.Find("article.mnl-card");
            card.GetAttribute("data-interactive").ShouldBe("true");
            card.GetAttribute("data-padding").ShouldBe("lg");
        }
    }

    [Test]
    public async Task GivenAnId_WhenThePanelIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlPanel>(new Dictionary<string, object?>
        {
            ["id"] = "category-panel",
            ["Mode"] = MnlPanelMode.Dialog,
            ["Header"] = Rendered.Text("Add category"),
            ["ChildContent"] = Rendered.Text("The form")
        });

        IElement dialog = document.Find("dialog.mnl-panel");

        ItShouldBeADialogThatMenloJsOpens();
        ItShouldBeNamedByItsTitle();
        ItShouldHaveACloseButton();

        void ItShouldBeADialogThatMenloJsOpens()
        {
            dialog.HasAttribute("data-mnl-panel").ShouldBeTrue();
            dialog.GetAttribute("data-mode").ShouldBe("dialog");
        }

        void ItShouldBeNamedByItsTitle()
        {
            dialog.GetAttribute("aria-labelledby").ShouldBe("category-panel-title");
            document.Find("#category-panel-title").TextContent.ShouldBe("Add category");
        }

        void ItShouldHaveACloseButton()
        {
            IElement close = document.Find("button[data-mnl-close]");
            close.GetAttribute("aria-label").ShouldBe("Close panel");
        }
    }

    [Test]
    [Arguments(-20, "0")]
    [Arguments(62.5, "62.5")]
    [Arguments(140, "100")]
    public async Task GivenAValue_WhenTheProgressBarIsRendered_ThenItIsClampedToAPercentage(double value, string expected)
    {
        IDocument document = await Rendered.ComponentAsync<MnlProgress>(new Dictionary<string, object?>
        {
            ["Value"] = (decimal)value,
            ["Tone"] = MnlTone.Warning,
            ["AriaLabel"] = "Budget 2026 spending this month"
        });

        IElement bar = document.Find("progress");

        bar.GetAttribute("value").ShouldBe(expected);
        bar.GetAttribute("max").ShouldBe("100");
        bar.GetAttribute("aria-label").ShouldBe("Budget 2026 spending this month");
        document.Find(".mnl-progress").GetAttribute("data-tone").ShouldBe("warning");
    }

    [Test]
    public async Task GivenATrend_WhenTheStatIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlStat>(new Dictionary<string, object?>
        {
            ["Label"] = "Total budget",
            ["Value"] = "R 12 000",
            ["Trend"] = "+4.8% vs last month",
            ["TrendDirection"] = MnlTrendDirection.Up,
            ["TrendTone"] = MnlTone.Success
        });

        ItShouldShowTheFigureAndItsLabel();
        ItShouldShowTheTrendAsABadge();

        void ItShouldShowTheFigureAndItsLabel()
        {
            document.Find(".mnl-stat__label").TextContent.ShouldBe("Total budget");
            document.Find(".mnl-stat__value").TextContent.ShouldBe("R 12 000");
        }

        void ItShouldShowTheTrendAsABadge()
        {
            IElement badge = document.Find(".mnl-badge");
            badge.GetAttribute("data-tone").ShouldBe("success");
            badge.GetAttribute("data-direction").ShouldBe("up");
            badge.TextContent.ShouldContain("+4.8% vs last month");
        }
    }

    [Test]
    [Arguments("Sam Example", "SE")]
    [Arguments("alex", "A")]
    [Arguments("  ", "?")]
    public async Task GivenAName_WhenTheAvatarIsRendered_ThenItShowsTheInitials(string name, string initials)
    {
        IDocument document = await Rendered.ComponentAsync<MnlAvatar>(new Dictionary<string, object?> { ["Name"] = name });

        IElement avatar = document.Find(".mnl-avatar");

        avatar.TextContent.Trim().ShouldBe(initials);
        avatar.GetAttribute("aria-label").ShouldBe(name);
    }

    [Test]
    public async Task GivenAnErrorAlert_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlAlert>(new Dictionary<string, object?>
        {
            ["Title"] = "We couldn't load your budgets.",
            ["ChildContent"] = Rendered.Text("Try again.")
        });

        ItShouldInterruptScreenReaders();

        void ItShouldInterruptScreenReaders()
        {
            IElement alert = document.Find(".mnl-alert");
            alert.GetAttribute("role").ShouldBe("alert");
            alert.GetAttribute("data-tone").ShouldBe("error");
            alert.TextContent.ShouldContain("We couldn't load your budgets.");
        }
    }

    [Test]
    public async Task GivenChildren_WhenTheDisclosureIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlDisclosure>(new Dictionary<string, object?>
        {
            ["Open"] = true,
            ["Summary"] = Rendered.Text("Housing"),
            ["ChildContent"] = Rendered.Text("Rent")
        });

        ItShouldBeAnOpenNativeDisclosure();

        void ItShouldBeAnOpenNativeDisclosure()
        {
            IElement details = document.Find("details.mnl-disclosure");
            details.HasAttribute("open").ShouldBeTrue();
            details.Find("summary").TextContent.ShouldContain("Housing");
            details.Find(".mnl-disclosure__content").TextContent.ShouldBe("Rent");
        }
    }

    [Test]
    public async Task GivenNoChildren_WhenTheDisclosureIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlDisclosure>(new Dictionary<string, object?>
        {
            ["Summary"] = Rendered.Text("Rent")
        });

        ItShouldBeAPlainRowThatCannotExpand();

        void ItShouldBeAPlainRowThatCannotExpand()
        {
            document.QuerySelector("details").ShouldBeNull();
            document.Find(".mnl-disclosure__summary").GetAttribute("data-static").ShouldBe("true");
        }
    }

    [Test]
    [Arguments(1, "H1", "display")]
    [Arguments(3, "H3", "xl")]
    [Arguments(9, "H4", "md")]
    public async Task GivenALevel_WhenTheHeadingIsRendered_ThenTheElementAndDefaultSizeFollowIt(int level, string tag, string size)
    {
        IDocument document = await Rendered.ComponentAsync<MnlHeading>(new Dictionary<string, object?>
        {
            ["Level"] = level,
            ["ChildContent"] = Rendered.Text("Title")
        });

        IElement heading = document.Find(".mnl-heading");

        heading.TagName.ShouldBe(tag);
        heading.GetAttribute("data-size").ShouldBe(size);
    }
}
