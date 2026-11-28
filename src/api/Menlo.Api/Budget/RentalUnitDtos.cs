namespace Menlo.Api.Budget;

/// <summary>
/// The body of <c>POST /api/rental-units</c>: the new rental unit's name.
/// </summary>
/// <param name="Name">Its name, 1 to 100 characters.</param>
public sealed record CreateRentalUnitRequest(string? Name);

/// <summary>
/// The body of <c>PATCH /api/rental-units/{id}</c>: what to change. A field left out is kept.
/// </summary>
/// <param name="Name">Its new name, 1 to 100 characters, or <see langword="null"/> to keep the name it has.</param>
public sealed record UpdateRentalUnitRequest(string? Name);

/// <summary>
/// A rental unit: a part of the property that the household lets to a tenant.
/// </summary>
/// <param name="Id">The rental unit's ID.</param>
/// <param name="Name">Its name.</param>
public sealed record RentalUnitDto(Guid Id, string Name);
