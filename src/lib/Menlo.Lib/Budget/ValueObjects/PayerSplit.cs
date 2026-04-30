using CSharpFunctionalExtensions;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Budget.ValueObjects;

/// <summary>
/// Represents a single allocation within a payer split.
/// </summary>
/// <param name="UserId">The household member responsible for this portion.</param>
/// <param name="Percent">The percentage of responsibility (1-100).</param>
public readonly record struct PayerAllocation(UserId UserId, int Percent);

/// <summary>
/// Value object representing how payment responsibility is split across household members.
/// Invariant: All percentages must sum to exactly 100%.
/// </summary>
public sealed class PayerSplit
{
    private readonly IReadOnlyList<PayerAllocation> _allocations;

    private PayerSplit(IReadOnlyList<PayerAllocation> allocations)
    {
        _allocations = allocations;
    }

    /// <summary>Gets the allocations in this split.</summary>
    public IReadOnlyList<PayerAllocation> Allocations => _allocations;

    /// <summary>
    /// Creates a PayerSplit after validating that percentages sum to 100% and all are positive.
    /// </summary>
    public static Result<PayerSplit, BudgetError> Create(IReadOnlyList<PayerAllocation> allocations)
    {
        if (allocations is null || allocations.Count == 0)
        {
            return new InvalidPayerSplitError("At least one payer allocation is required.");
        }

        if (allocations.Any(a => a.Percent <= 0))
        {
            return new InvalidPayerSplitError("All payer percentages must be positive.");
        }

        int total = allocations.Sum(a => a.Percent);
        if (total != 100)
        {
            return new InvalidPayerSplitError($"Payer split percentages must sum to 100%, but got {total}%.");
        }

        // Check for duplicate user IDs
        if (allocations.Select(a => a.UserId).Distinct().Count() != allocations.Count)
        {
            return new InvalidPayerSplitError("Duplicate user IDs are not allowed in a payer split.");
        }

        return new PayerSplit(allocations.ToList());
    }

    public override bool Equals(object? obj) =>
        obj is PayerSplit other && _allocations.SequenceEqual(other._allocations);

    public override int GetHashCode()
    {
        HashCode hash = new();
        foreach (PayerAllocation alloc in _allocations)
        {
            hash.Add(alloc);
        }
        return hash.ToHashCode();
    }
}
