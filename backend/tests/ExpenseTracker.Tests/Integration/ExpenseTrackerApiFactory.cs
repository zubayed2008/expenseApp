using ExpenseTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ExpenseTracker.Tests.Integration;

/// <summary>
/// Boots the API against a real PostgreSQL 16 container (NFR-050) and
/// applies EF Core migrations on startup. The container is created lazily so
/// that tests skip cleanly (SkippableFact) when no Docker daemon exists.
/// </summary>
public sealed class ExpenseTrackerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _db;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            _db?.GetConnectionString() ?? "Host=localhost;Port=5432;Database=unused;Username=unused;Password=unused");
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }

    /// <summary>Scoped AppDbContext for direct data assertions (Docker only).</summary>
    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public async Task InitializeAsync()
    {
        if (!DockerEnvironment.IsAvailable)
        {
            return;
        }

        _db = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        await _db.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_db is not null)
        {
            await _db.DisposeAsync();
        }
    }
}
