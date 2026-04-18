using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;

namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Value object representing a validated, trimmed budget category name.
/// </summary>
public sealed class CategoryName
{
    private CategoryName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the trimmed category name.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new CategoryName after validating it is non-empty and trimming whitespace.
    /// </summary>
    /// <param name="name">The raw category name string.</param>
    /// <returns>Success with CategoryName if valid; Failure with BudgetError if empty or whitespace.</returns>
    public static Result<CategoryName, BudgetError> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new InvalidBudgetDataError("Category name cannot be empty.");
        }

        return new CategoryName(name.Trim());
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is CategoryName other && string.Equals(Value, other.Value, StringComparison.Ordinal);

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
}
