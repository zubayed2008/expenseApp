using System.Reflection;
using ExpenseTracker.Domain;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseTracker.Infrastructure.Migrations;

/// <summary>
/// Applies the numbered SQL scripts embedded in ExpenseTracker.Domain
/// (000_create_schema_migrations.sql, 001_create_users.sql, ...), skipping
/// versions already recorded in schema_migrations. Each script runs inside
/// its own transaction together with its bookkeeping insert.
///
/// The scripts are also meant to be applied manually (psql / DBeaver / CI
/// job): check where you are with
///   SELECT version, name, applied_at FROM schema_migrations ORDER BY version;
/// </summary>
public sealed class SqlMigrationRunner(AppDbContext db)
{
    public sealed record MigrationScript(int Version, string Name, string Sql);

    public async Task ApplyAllAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);

        // Serialize concurrent appliers (parallel test hosts, app replicas).
        // Session-level advisory lock: released on unlock or session end.
        const int LockKey = 727_272;
        await ExecuteNonQueryAsync($"SELECT pg_advisory_lock({LockKey});", cancellationToken);
        try
        {
            await EnsureTrackingTableAsync(cancellationToken);
            var applied = await ReadAppliedVersionsAsync(cancellationToken);

            foreach (var script in LoadScripts())
            {
                if (applied.Contains(script.Version))
                {
                    continue;
                }

                await ApplyScriptAsync(script, cancellationToken);
                applied.Add(script.Version);
            }
        }
        finally
        {
            await ExecuteNonQueryAsync($"SELECT pg_advisory_unlock({LockKey});", cancellationToken);
        }
    }

    /// <summary>Numbered scripts, ordered by version, embedded in the Domain assembly.</summary>
    public static IReadOnlyList<MigrationScript> LoadScripts()
    {
        var assembly = typeof(DefaultCategories).Assembly;
        var resourceNames = assembly
            .GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var scripts = new List<MigrationScript>();
        foreach (var resourceName in resourceNames)
        {
            // "ExpenseTracker.Domain.Migrations.001_create_users.sql" -> "001_create_users"
            var withoutExtension = resourceName[..^".sql".Length];
            var fileName = withoutExtension[(withoutExtension.LastIndexOf('.') + 1)..];
            var version = ExtractVersion(fileName);
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Missing embedded resource {resourceName}.");
            using var reader = new StreamReader(stream);
            scripts.Add(new MigrationScript(version, fileName, reader.ReadToEnd()));
        }

        return scripts.OrderBy(script => script.Version).ToList();
    }

    private static int ExtractVersion(string fileName)
    {
        var digits = new string(fileName.TakeWhile(char.IsDigit).ToArray());
        if (!int.TryParse(digits, out var version) || version < 0)
        {
            throw new InvalidOperationException(
                $"Migration file '{fileName}' must start with a number, e.g. 001_create_users.sql.");
        }

        return version;
    }

    private async Task EnsureTrackingTableAsync(CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version     integer PRIMARY KEY,
                name        text        NOT NULL,
                applied_at  timestamptz NOT NULL DEFAULT now()
            );
            """,
            cancellationToken);
    }

    private async Task<HashSet<int>> ReadAppliedVersionsAsync(CancellationToken cancellationToken)
    {
        var versions = new HashSet<int>();
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version FROM schema_migrations;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            versions.Add(reader.GetInt32(0));
        }

        return versions;
    }

    private async Task ApplyScriptAsync(MigrationScript script, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Cannot apply migrations inside an existing transaction.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await ExecuteNonQueryAsync(script.Sql, cancellationToken, transaction);
        await ExecuteNonQueryAsync(
            $"INSERT INTO schema_migrations (version, name) VALUES ({script.Version}, '{script.Name.Replace("'", "''")}');",
            cancellationToken,
            transaction);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ExecuteNonQueryAsync(
        string sql,
        CancellationToken cancellationToken,
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        if (transaction is not null)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
