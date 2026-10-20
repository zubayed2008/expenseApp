using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ExpenseTracker.Api.Data;

/// <summary>
/// Design-time factory used by `dotnet ef migrations ...`. Reads the
/// connection string from EXPENSE_TRACKER_CONNECTION or falls back to the
/// local development default.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=expensetracker;Username=expensetracker;Password=expensetracker";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("EXPENSE_TRACKER_CONNECTION")
            ?? FallbackConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
