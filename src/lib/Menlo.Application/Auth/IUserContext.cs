using Menlo.Lib.Auth.Entities;
using Microsoft.EntityFrameworkCore;

namespace Menlo.Application.Auth;

public interface IUserContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}


