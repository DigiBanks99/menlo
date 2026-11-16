namespace Menlo.Ui.Tests.Components;

public sealed class MnlButtonTests
{
    [Test]
    public async Task GivenDefaults_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlButton>(new Dictionary<string, object?>
        {
            ["ChildContent"] = Rendered.Text("Save")
        });

        IElement button = document.Find("button.mnl-button");

        ItShouldBeAPrimaryMediumButton();
        ItShouldNotSubmitAForm();
        ItShouldShowItsLabel();

        void ItShouldBeAPrimaryMediumButton()
        {
            button.GetAttribute("data-variant").ShouldBe("primary");
            button.GetAttribute("data-size").ShouldBe("md");
        }

        void ItShouldNotSubmitAForm() => button.GetAttribute("type").ShouldBe("button");

        void ItShouldShowItsLabel() => button.Find(".mnl-button__label").TextContent.ShouldBe("Save");
    }

    [Test]
    public async Task GivenAnHref_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlButton>(new Dictionary<string, object?>
        {
            ["Href"] = "/budgets",
            ["Variant"] = MnlButtonVariant.Secondary,
            ["ChildContent"] = Rendered.Text("View Budgets")
        });

        ItShouldBeALinkStyledAsAButton();

        void ItShouldBeALinkStyledAsAButton()
        {
            IElement link = document.Find("a.mnl-button");
            link.GetAttribute("href").ShouldBe("/budgets");
            link.GetAttribute("data-variant").ShouldBe("secondary");
            document.QuerySelector("button").ShouldBeNull();
        }
    }

    [Test]
    public async Task GivenADisabledLink_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlButton>(new Dictionary<string, object?>
        {
            ["Href"] = "/budgets",
            ["Disabled"] = true
        });

        ItShouldNotBeFollowable();

        void ItShouldNotBeFollowable()
        {
            IElement link = document.Find("a.mnl-button");
            link.HasAttribute("href").ShouldBeFalse();
            link.GetAttribute("aria-disabled").ShouldBe("true");
        }
    }

    [Test]
    public async Task GivenHtmxAttributesAndAClass_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlButton>(new Dictionary<string, object?>
        {
            ["hx-post"] = "/budgets/2027",
            ["data-testid"] = "create-budget-button",
            ["class"] = "extra"
        });

        IElement button = document.Find("button");

        ItShouldPutTheAttributesOnTheButton();
        ItShouldKeepItsOwnClassBesideTheCallers();

        void ItShouldPutTheAttributesOnTheButton()
        {
            button.GetAttribute("hx-post").ShouldBe("/budgets/2027");
            button.GetAttribute("data-testid").ShouldBe("create-budget-button");
        }

        void ItShouldKeepItsOwnClassBesideTheCallers() => button.ClassList.ShouldBe(["mnl-button", "extra"]);
    }

    [Test]
    public async Task GivenAnIcon_WhenRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlButton>(new Dictionary<string, object?>
        {
            ["Icon"] = MnlIconName.Plus,
            ["Type"] = MnlButtonType.Submit
        });

        ItShouldDrawTheIconAsDecoration();
        ItShouldSubmitItsForm();

        void ItShouldDrawTheIconAsDecoration() =>
            document.Find("button svg.mnl-icon").GetAttribute("aria-hidden").ShouldBe("true");

        void ItShouldSubmitItsForm() => document.Find("button").GetAttribute("type").ShouldBe("submit");
    }
}
