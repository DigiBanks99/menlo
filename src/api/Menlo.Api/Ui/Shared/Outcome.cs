using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Menlo.Api.Ui.Shared;

/// <summary>
/// What a call to an API handler produced, in a shape a page can render: a value, or a failure with a
/// status code and a message. The UI calls the same handlers as the JSON API, in process, and reads
/// their results through this type.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
internal sealed record Outcome<T>
{
    private Outcome(T? value, int status, string? error)
    {
        Value = value;
        Status = status;
        Error = error;
    }

    /// <summary>The value, when the call succeeded.</summary>
    public T? Value { get; }

    /// <summary>The HTTP status the handler chose.</summary>
    public int Status { get; }

    /// <summary>A message for the person, when the call failed.</summary>
    public string? Error { get; }

    /// <summary>Whether the call succeeded.</summary>
    public bool IsSuccess => Status is >= 200 and < 300;

    /// <summary>Whether the handler reported that the thing asked for doesn't exist (or its feature is off).</summary>
    public bool IsNotFound => Status == StatusCodes.Status404NotFound;

    /// <summary>Whether the handler reported a conflict with existing data, such as a duplicate name.</summary>
    public bool IsConflict => Status == StatusCodes.Status409Conflict;

    /// <summary>
    /// Reads a handler's result.
    /// </summary>
    public static Outcome<T> From(IResult result)
    {
        int status = (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK;

        if (status is >= 200 and < 300)
        {
            T? value = result is IValueHttpResult { Value: T typed } ? typed : default;
            return new Outcome<T>(value, status, null);
        }

        return new Outcome<T>(default, status, MessageFor(result, status));
    }

    private static string MessageFor(IResult result, int status)
    {
        if (result is ProblemHttpResult problem)
        {
            return FirstNonBlank(problem.ProblemDetails.Detail, problem.ProblemDetails.Title) ?? DefaultMessage(status);
        }

        // Conflicts carry an anonymous { Code, Message } object
        if (result is IValueHttpResult { Value: { } body })
        {
            if (body is ProblemDetails details)
            {
                return FirstNonBlank(details.Detail, details.Title) ?? DefaultMessage(status);
            }

            PropertyInfo? message = body.GetType().GetProperty("Message");
            if (message?.GetValue(body) is string text && !string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return DefaultMessage(status);
    }

    private static string? FirstNonBlank(params string?[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate));

    private static string DefaultMessage(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => "You need to sign in again.",
        StatusCodes.Status403Forbidden => "You don't have permission to do that.",
        StatusCodes.Status404NotFound => "That wasn't found. It may have been removed, or the feature isn't switched on.",
        StatusCodes.Status409Conflict => "That conflicts with something that already exists.",
        _ => "Something went wrong. Please try again."
    };
}
