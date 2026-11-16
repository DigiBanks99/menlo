namespace Menlo.Ui.Tests.Layouts;

public sealed class ShellTests
{
    private static readonly MnlNavItem[] NavItems =
    [
        new("Home", "/", MnlIconName.House),
        new("Budgets", "/budgets", MnlIconName.Wallet)
    ];

    [Test]
    [Arguments("/", "/", true)]
    [Arguments("/", "/budgets", false)]
    [Arguments("/budgets", "/budgets", true)]
    [Arguments("/budgets", "/budgets/123", true)]
    [Arguments("/budgets", "/budgets-archive", false)]
    [Arguments("/budgets", "/", false)]
    public void GivenANavItem_WhenCheckedAgainstAPath_ThenItIsCurrentForItsOwnSection(string href, string path, bool expected)
    {
        MnlNavItem item = new("Item", href, MnlIconName.House);

        item.IsCurrent(path).ShouldBe(expected);
    }

    [Test]
    public async Task GivenABudgetPath_WhenTheAppShellIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlAppShell>(new Dictionary<string, object?>
        {
            ["NavItems"] = NavItems,
            ["CurrentPath"] = "/budgets/123",
            ["UserName"] = "Sam Example",
            ["ChildContent"] = Rendered.Text("Page")
        });

        ItShouldMarkOnlyBudgetsAsCurrent();
        ItShouldShowWhoIsSignedIn();
        ItShouldRenderThePageInMain();

        void ItShouldMarkOnlyBudgetsAsCurrent()
        {
            document.Find("a[href='/budgets']").GetAttribute("aria-current").ShouldBe("page");
            document.Find(".mnl-app-shell__nav-link[href='/']").HasAttribute("aria-current").ShouldBeFalse();
        }

        void ItShouldShowWhoIsSignedIn() => document.Find(".mnl-avatar").GetAttribute("aria-label").ShouldBe("Sam Example");

        void ItShouldRenderThePageInMain() => document.Find("main").TextContent.Trim().ShouldBe("Page");
    }

    [Test]
    public async Task GivenAToast_WhenTheDocumentIsRendered()
    {
        IDocument document = await Rendered.ComponentAsync<MnlDocument>(new Dictionary<string, object?>
        {
            ["Title"] = "Budgets · Menlo",
            ["StylesBundle"] = "Menlo.Api.styles.css",
            ["Toasts"] = new MnlToastMessage[] { new("Budget 2027 created.", MnlTone.Success) },
            ["ChildContent"] = Rendered.Text("Page")
        });

        ItShouldHaveTheTitle();
        ItShouldLoadEveryScriptAndStylesheetFromThisOrigin();
        ItShouldBoostNavigationWithoutLettingOtherAttributesInherit();
        ItShouldHaveAPanelHost();
        ItShouldShowTheToast();

        void ItShouldHaveTheTitle() => document.Title.ShouldBe("Budgets · Menlo");

        void ItShouldLoadEveryScriptAndStylesheetFromThisOrigin()
        {
            document.QuerySelectorAll("script").ShouldAllBe(script => script.GetAttribute("src")!.StartsWith("/_content/Menlo.Ui/"));
            document.QuerySelectorAll("link[rel='stylesheet']").ShouldAllBe(link => link.GetAttribute("href")!.StartsWith('/'));
            document.QuerySelectorAll("link[rel='stylesheet']").ShouldContain(link => link.GetAttribute("href")!.StartsWith("/Menlo.Api.styles.css?v="));
        }

        void ItShouldBoostNavigationWithoutLettingOtherAttributesInherit()
        {
            document.Body!.GetAttribute("hx-boost").ShouldBe("true");
            document.Body.GetAttribute("hx-inherit").ShouldBe("hx-boost");
            document.Find("meta[name='htmx-config']").GetAttribute("content")!.ShouldContain("\"disableInheritance\":true");
        }

        void ItShouldHaveAPanelHost() => document.Find(MnlPanel.HostSelector).ShouldNotBeNull();

        void ItShouldShowTheToast()
        {
            IElement toast = document.Find("#mnl-toast-region > [data-mnl-toast]");
            toast.GetAttribute("data-tone").ShouldBe("success");
            toast.TextContent.ShouldContain("Budget 2027 created.");
        }
    }
}
