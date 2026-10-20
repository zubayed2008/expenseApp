using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ExpenseTracker.Tests.Integration;

/// <summary>
/// Boots the API against a real PostgreSQL 16 container (NFR-050) and
/// applies the numbered SQL migrations on startup. The container is created
/// lazily so tests skip cleanly (SkippableFact) when no Docker daemon exists.
///
/// An existing PostgreSQL can be provided instead of a container by setting
/// EXPENSE_TRACKER_TEST_CONNECTION (used to verify against a local server
/// where Docker is unavailable).
/// </summary>
public sealed class ExpenseTrackerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _db;
    private string? _externalConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            _externalConnectionString
                ?? _db?.GetConnectionString()
                ?? "Host=localhost;Port=5432;Database=unused;Username=unused;Password=unused");
        builder.UseSetting("Database:ApplySqlMigrations", "true");
    }

    /// <summary>Scoped AppDbContext for direct data assertions.</summary>
    public AppDbContext CreateDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public Task InitializeAsync()
    {
        _externalConnectionString =
            Environment.GetEnvironmentVariable("EXPENSE_TRACKER_TEST_CONNECTION");

        if (_externalConnectionString is not null)
        {
            return Task.CompletedTask;
        }

        if (!DockerEnvironment.IsAvailable)
        {
            return Task.CompletedTask;
        }

        _db = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();
        return _db.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_db is not null)
        {
            await _db.DisposeAsync();
        }
    }
}

