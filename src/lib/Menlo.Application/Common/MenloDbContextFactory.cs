using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Menlo.Application.Common;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add/remove/update).
/// Not used at runtime.
/// </summary>
internal sealed class MenloDbContextFactory : IDesignTimeDbContextFactory<MenloDbContext>
{
    public MenloDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<MenloDbContext> options = new DbContextOptionsBuilder<MenloDbContext>()
            .UseNpgsql("Host=localhost;Database=menlo;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new MenloDbContext(options);
    }
}
