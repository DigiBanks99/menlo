using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// Turns a view model into a response. Endpoints end with one of these.
/// </summary>
internal static class Render
{
    /// <summary>
    /// Renders a full page: a component with a <c>Model</c> parameter and a <c>Page</c> parameter
    /// (the <see cref="PageContext"/>).
    /// </summary>
    public static RazorComponentResult<TPage> Page<TPage>(HttpContext context, object? model = null, int statusCode = StatusCodes.Status200OK)
        where TPage : IComponent
    {
        Dictionary<string, object?> parameters = new() { ["Page"] = PageContext.From(context) };
        if (model is not null)
        {
            parameters["Model"] = model;
        }

        return new RazorComponentResult<TPage>(parameters) { StatusCode = statusCode };
    }

    /// <summary>
    /// Renders a fragment: a component with a <c>Model</c> parameter, without the document around it.
    /// </summary>
    public static RazorComponentResult<TFragment> Fragment<TFragment>(object model, int statusCode = StatusCodes.Status200OK)
        where TFragment : IComponent =>
        new(new Dictionary<string, object?> { ["Model"] = model }) { StatusCode = statusCode };

    /// <summary>
    /// Renders a form again with its validation errors. htmx swaps 422 responses, so the form replaces itself.
    /// </summary>
    public static RazorComponentResult<TFragment> Invalid<TFragment>(object model)
        where TFragment : IComponent =>
        Fragment<TFragment>(model, StatusCodes.Status422UnprocessableEntity);
}
