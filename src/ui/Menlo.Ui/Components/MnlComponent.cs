using Menlo.Ui.Utilities;
using Microsoft.AspNetCore.Components;

namespace Menlo.Ui.Components;

/// <summary>
/// Base class for design-system components. It passes unmatched attributes (<c>id</c>, <c>data-testid</c>,
/// <c>hx-*</c>, <c>aria-*</c>) to the component's root element and merges a caller's <c>class</c> with the
/// component's own.
/// </summary>
public abstract class MnlComponent : ComponentBase
{
    /// <summary>
    /// Attributes the component doesn't declare. They are rendered on its root element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? Attributes { get; set; }

    /// <summary>
    /// Returns the component's class followed by any <c>class</c> the caller passed.
    /// </summary>
    protected string RootClass(string componentClass)
    {
        string? extra = Attributes is not null && Attributes.TryGetValue("class", out object? value)
            ? value?.ToString()
            : null;

        return Css.Classes(componentClass, extra);
    }
}
