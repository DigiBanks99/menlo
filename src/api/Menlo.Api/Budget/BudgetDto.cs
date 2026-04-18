namespace Menlo.Api.Budget;

public sealed record MoneyDto(decimal Amount, string Currency);

public sealed record CategoryNodeDto(Guid Id, string Name, Guid? ParentId, MoneyDto PlannedMonthlyAmount);

public sealed record BudgetDto(
    Guid Id,
    int Year,
    Guid HouseholdId,
    string Status,
    IReadOnlyCollection<CategoryNodeDto> Categories,
    MoneyDto TotalPlannedMonthlyAmount);
