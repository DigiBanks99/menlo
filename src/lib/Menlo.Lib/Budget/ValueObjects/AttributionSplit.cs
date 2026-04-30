using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Enums;
using Menlo.Lib.Budget.Errors;

namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a single allocation within an attribution split.
/// </summary>
/// <param name="Attribution">The attribution purpose (Main, Rental, ServiceProvider).</param>
/// <param name="Percent">The percentage attributed (1-100).</param>
public readonly record struct AttributionAllocation(Attribution Attribution, int Percent);

/// <summary>
/// Value object representing how a budget item's cost is attributed across purposes.
/// Invariant: All percentages must sum to exactly 100%.
/// </summary>
public sealed class AttributionSplit
{
    private readonly IReadOnlyList<AttributionAllocation> _allocations;

    private AttributionSplit(IReadOnlyList<AttributionAllocation> allocations)
    {
        _allocations = allocations;
    }

    /// <summary>Gets the allocations in this split.</summary>
    public IReadOnlyList<AttributionAllocation> Allocations => _allocations;

    /// <summary>
    /// Creates an AttributionSplit after validating that percentages sum to 100% and all are positive.
    /// </summary>
    public static Result<AttributionSplit, BudgetError> Create(IReadOnlyList<AttributionAllocation> allocations)
    {
        if (allocations is null || allocations.Count == 0)
        {
            return new InvalidAttributionSplitError("At least one attribution allocation is required.");
        }

        if (allocations.Any(a => a.Percent <= 0))
        {
            return new InvalidAttributionSplitError("All attribution percentages must be positive.");
        }

        int total = allocations.Sum(a => a.Percent);
        if (total != 100)
        {
            return new InvalidAttributionSplitError($"Attribution split percentages must sum to 100%, but got {total}%.");
        }

        // Check for duplicate attributions
        if (allocations.Select(a => a.Attribution).Distinct().Count() != allocations.Count)
        {
            return new InvalidAttributionSplitError("Duplicate attributions are not allowed in an attribution split.");
        }

        return new AttributionSplit(allocations.ToList());
    }

    public override bool Equals(object? obj) =>
        obj is AttributionSplit other && _allocations.SequenceEqual(other._allocations);

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (AttributionAllocation alloc in _allocations)
        {
            hash.Add(alloc);
        }
        return hash.ToHashCode();
    }
}
