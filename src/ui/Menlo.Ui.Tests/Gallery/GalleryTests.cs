using Menlo.Ui.Gallery;

namespace Menlo.Ui.Tests.Gallery;

/// <summary>
/// The gallery renders every component, so rules checked here hold for the whole design system.
/// </summary>
public sealed class GalleryTests
{
    private static readonly Dictionary<string, object?> Parameters = new()
    {
        ["PanelUrl"] = "/design/panel",
        ["ToastUrl"] = "/design/toast"
    };

    [Test]
    public async Task GivenTheGallery_WhenRendered_ThenNothingNeedsAnInlineStyleOrScript()
    {
        IDocument document = await Rendered.ComponentAsync<MnlGallery>(Parameters);

        ItShouldHaveNoInlineStyles();
        ItShouldHaveNoInlineScripts();
        ItShouldHaveNoInlineEventHandlers();

        // The Content Security Policy is "style-src 'self'; script-src 'self'": any of these would be blocked
        void ItShouldHaveNoInlineStyles()
        {
            document.QuerySelectorAll("[style]").ShouldBeEmpty();
            document.QuerySelectorAll("style").ShouldBeEmpty();
        }

        void ItShouldHaveNoInlineScripts() => document.QuerySelectorAll("script").ShouldBeEmpty();

        void ItShouldHaveNoInlineEventHandlers() =>
            document.All
                .SelectMany(element => element.Attributes)
                .Where(attribute => attribute.Name.StartsWith("on", StringComparison.OrdinalIgnoreCase) || attribute.Name.StartsWith("hx-on", StringComparison.OrdinalIgnoreCase))
                .ShouldBeEmpty();
    }

    [Test]
    public async Task GivenTheGallery_WhenRendered_ThenEverySectionAndIconIsShown()
    {
        IDocument document = await Rendered.ComponentAsync<MnlGallery>(Parameters);

        ItShouldHaveEverySection();
        ItShouldLinkToEverySection();
        ItShouldShowEveryIcon();
        ItShouldShowTheCodeForItsExamples();

        void ItShouldHaveEverySection() =>
            document.QuerySelectorAll(".gallery-section").Select(section => section.Id)
                .ShouldBe(["foundations", "actions", "surfaces", "forms", "layout", "charts"]);

        void ItShouldLinkToEverySection() =>
            document.QuerySelectorAll(".gallery__links a").Select(link => link.GetAttribute("href"))
                .ShouldBe(["#foundations", "#actions", "#surfaces", "#forms", "#layout", "#charts"]);

        void ItShouldShowEveryIcon() =>
            document.QuerySelectorAll("#icons svg.mnl-icon").Length.ShouldBe(Enum.GetValues<MnlIconName>().Length);

        void ItShouldShowTheCodeForItsExamples() =>
            document.QuerySelectorAll(".gallery-example__code").Length.ShouldBeGreaterThan(20);
    }

    [Test]
    public async Task GivenTheGallery_WhenRendered_ThenFormControlsAreLabelledAndIdsAreUnique()
    {
        IDocument document = await Rendered.ComponentAsync<MnlGallery>(Parameters);

        ItShouldGiveEveryControlAnAccessibleName();
        ItShouldNotRepeatAnId();

        void ItShouldGiveEveryControlAnAccessibleName()
        {
            HashSet<string?> labelled = [.. document.QuerySelectorAll("label[for]").Select(label => label.GetAttribute("for"))];

            // Controls inside a <template> are blueprints for rows added later, so they are not in the document yet
            document.QuerySelectorAll("input, select")
                .Where(control => control.Closest("label") is null
                    && !control.HasAttribute("aria-label")
                    && !labelled.Contains(control.Id))
                .Select(control => control.OuterHtml)
                .ShouldBeEmpty();
        }

        void ItShouldNotRepeatAnId() =>
            document.QuerySelectorAll("[id]")
                .GroupBy(element => element.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ShouldBeEmpty();
    }
}
