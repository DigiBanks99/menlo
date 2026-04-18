using Menlo.Lib.Auth.Entities;
using Menlo.Lib.Common.ValueObjects;
using System.Reflection;

namespace Menlo.Application.Tests.Fixtures;

internal static class UserTestHelper
{
    private static readonly PropertyInfo HouseholdIdProperty =
        typeof(User).GetProperty(nameof(User.HouseholdId), BindingFlags.Public | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find HouseholdId property on User.");

    public static void SetHouseholdId(User user, HouseholdId householdId)
    {
        HouseholdIdProperty.SetValue(user, householdId);
    }
}
