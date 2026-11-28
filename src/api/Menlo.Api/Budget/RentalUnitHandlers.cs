using CSharpFunctionalExtensions;
using Menlo.Application.Budget;
using Menlo.Lib.Budget.Entities;
using Menlo.Lib.Budget.Errors;
using Menlo.Lib.Budget.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Api.Budget;

/// <summary>
/// What can be done with the household's rental units. The JSON endpoints call these, and so will the budget page.
/// </summary>
public sealed class RentalUnitHandlers(IBudgetContext context)
{
    /// <summary>
    /// Adds a rental unit to the household.
    /// </summary>
    /// <param name="name">Its name.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The new rental unit, or why it can't be added.</returns>
    public async Task<Result<RentalUnitDto, BudgetError>> CreateAsync(string? name, CancellationToken cancellationToken)
    {
        Result<RentalUnit, BudgetError> created = RentalUnit.Create(name);
        if (created.IsFailure)
        {
            return created.Error;
        }

        context.RentalUnits.Add(created.Value);
        await context.SaveChangesAsync(cancellationToken);

        return ToDto(created.Value);
    }

    /// <summary>
    /// Lists the household's rental units, by name.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>Every rental unit.</returns>
    public async Task<IReadOnlyList<RentalUnitDto>> ListAsync(CancellationToken cancellationToken)
    {
        List<RentalUnit> units = await context.RentalUnits
            .AsNoTracking()
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.CreatedAt)
            .ToListAsync(cancellationToken);

        return units.ConvertAll(ToDto);
    }

    /// <summary>
    /// Gets a rental unit.
    /// </summary>
    /// <param name="id">The rental unit's ID.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The rental unit, or <see langword="null"/> when there is none with that ID.</returns>
    public async Task<RentalUnitDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        RentalUnitId unitId = new(id);
        RentalUnit? unit = await context.RentalUnits
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == unitId, cancellationToken);

        return unit is null ? null : ToDto(unit);
    }

    /// <summary>
    /// Changes a rental unit. Renaming it is an edit, not a dated change, and never changes its identity.
    /// </summary>
    /// <param name="id">The rental unit's ID.</param>
    /// <param name="name">Its new name, or <see langword="null"/> to keep the name it has.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>
    /// The changed rental unit; <see langword="null"/> when there is none with that ID; or why it can't be changed.
    /// </returns>
    public async Task<Result<RentalUnitDto?, BudgetError>> UpdateAsync(
        Guid id,
        string? name,
        CancellationToken cancellationToken)
    {
        RentalUnitId unitId = new(id);
        RentalUnit? unit = await context.RentalUnits.SingleOrDefaultAsync(u => u.Id == unitId, cancellationToken);
        if (unit is null)
        {
            return Result.Success<RentalUnitDto?, BudgetError>(null);
        }

        if (name is not null)
        {
            UnitResult<BudgetError> renamed = unit.Rename(name);
            if (renamed.IsFailure)
            {
                return renamed.Error;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return ToDto(unit);
    }

    private static RentalUnitDto ToDto(RentalUnit unit) => new(unit.Id.Value, unit.Name);
}
