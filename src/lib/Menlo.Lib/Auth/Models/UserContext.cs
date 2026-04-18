using Menlo.Lib.Common.ValueObjects;

namespace Menlo.Lib.Auth.Models;

public sealed record UserContext(UserId UserId, HouseholdId HouseholdId);
