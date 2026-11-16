using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Menlo.Ui.Charts;

/// <summary>
/// Renders an SVG <c>&lt;text&gt;</c> element. Razor reserves the <c>&lt;text&gt;</c> tag for its own
/// use, so charts can't write it directly in markup. Every attribute passed is rendered as given.
/// </summary>
public sealed class MnlSvgText : ComponentBase
{
    /// <summary>The attributes of the text element: class, x, y, text-anchor and so on.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? Attributes { get; set; }

    /// <summary>The text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "text");
        builder.AddMultipleAttributes(1, Attributes!);
        builder.AddContent(2, ChildContent);
        builder.CloseElement();
    }
}
