using Menlo.Lib.Common.Abstractions;

namespace Menlo.Lib.Common.Errors;

/// <summary>
/// Base class for all Money-related domain errors.
/// </summary>
public abstract class MoneyError(string code, string message) : Error(code, message);
