using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FraudDetection.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add/update` construct the DbContext without spinning up
/// the full application host. The connection string only needs to be valid enough for
/// EF's Npgsql provider to generate SQL — it is never actually connected to for
/// `migrations add`, and can be overridden via the FRAUDDETECTION_DB_CONNECTION env
/// var for `database update`.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FraudDetectionDbContext>
{
    public FraudDetectionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FRAUDDETECTION_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=frauddetection;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<FraudDetectionDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(FraudDetectionDbContext).Assembly.FullName));

        return new FraudDetectionDbContext(optionsBuilder.Options);
    }
}
