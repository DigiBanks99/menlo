using Menlo.Lib.Auth.Entities;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Auth;

public interface IHouseholdContext
{
    DbSet<Household> Households { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
