namespace Menlo.Lib.Common.Abstractions;

/// <summary>
/// Base class for all domain errors.
/// Errors represent business rule violations or domain-specific failure conditions.
/// Use Result&lt;T&gt; from CSharpFunctionalExtensions with Error for predictable error handling.
/// </summary>
public abstract class Error
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> class.
    /// </summary>
    /// <param name="code">Machine-readable error code (e.g., "BUDGET_001"). Must not be null or whitespace.</param>
    /// <param name="message">Human-readable error message. Must not be null or whitespace.</param>
    protected Error(string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Error code cannot be null or whitespace.", nameof(code));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Error message cannot be null or whitespace.", nameof(message));

        Code = code;
        Message = message;
    }

    /// <summary>
    /// Gets the machine-readable error code.
    /// Format: {DOMAIN}_{NUMBER} (e.g., BUDGET_001, PLANNING_005).
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable error message suitable for display to users.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Returns a string representation of this error.
    /// </summary>
    public override string ToString() => $"[{Code}] {Message}";
}
