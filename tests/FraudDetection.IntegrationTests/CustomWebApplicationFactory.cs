using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace FraudDetection.IntegrationTests;

/// <summary>
/// Boots the real API host (Application + Infrastructure DI, controllers, exception
/// handling — everything Program.cs wires up) against a disposable PostgreSQL
/// container, so these tests exercise the actual stack end to end rather than a
/// mocked slice of it. Requires a working Docker daemon on the machine running the
/// tests; see the README for how to run them.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("frauddetection_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:FraudDetectionDb"] = _postgres.GetConnectionString()
            });
        });
    }
}
