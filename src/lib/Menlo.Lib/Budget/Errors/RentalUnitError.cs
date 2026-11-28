using Menlo.Lib.Budget.Entities;

namespace Menlo.Lib.Budget.Errors;

/// <summary>
/// Error indicating a name that can't be a rental unit's name: it is blank, or longer than
/// <see cref="RentalUnit.MaxNameLength"/> characters.
/// </summary>
/// <param name="name">The name given.</param>
public sealed class InvalidRentalUnitNameError(string? name)
    : BudgetError(
        "RentalUnit.InvalidName",
        $"A rental unit needs a name of 1 to {RentalUnit.MaxNameLength} characters.")
{
    /// <summary>
    /// Gets the name given.
    /// </summary>
    public string? Name { get; } = name;
}
