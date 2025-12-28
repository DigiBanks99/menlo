using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Common.Errors;

/// <summary>
/// Error raised when currency code is null or empty.
/// </summary>
public sealed class EmptyCurrencyError()
    : Error("MONEY_000", "Currency code cannot be null or empty");
