using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ERPSystem.Infrastructure.Seed;

/// <summary>
/// Drops and recreates the configured PostgreSQL database, then reapplies EF migrations and baseline seed.
/// </summary>
public static class FullDatabaseReset
{
    public static async Task RecreateAndSeedAsync(
        ErpDbContext context,
        ILogger logger,
        Func<Task> migrateAndSeedAsync,
        CancellationToken cancellationToken = default)
    {
        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database
            ?? throw new InvalidOperationException("Connection string must include a Database name.");

        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Database name is empty.");

        logger.LogWarning("Recreating database {Database} on {Host}:{Port}...",
            databaseName, builder.Host, builder.Port);

        await context.Database.CloseConnectionAsync();

        builder.Database = "postgres";
        await using (var admin = new NpgsqlConnection(builder.ConnectionString))
        {
            await admin.OpenAsync(cancellationToken);

            await using (var terminate = admin.CreateCommand())
            {
                terminate.CommandText = """
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = @database
                      AND pid <> pg_backend_pid();
                    """;
                terminate.Parameters.AddWithValue("database", databaseName);
                await terminate.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var drop = admin.CreateCommand())
            {
                drop.CommandText = $"""DROP DATABASE IF EXISTS "{databaseName}";""";
                await drop.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var create = admin.CreateCommand())
            {
                var owner = string.IsNullOrWhiteSpace(builder.Username) ? "postgres" : builder.Username;
                create.CommandText = $"""CREATE DATABASE "{databaseName}" OWNER "{owner}";""";
                await create.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        logger.LogInformation("Database {Database} recreated. Applying migrations and seed...", databaseName);
        await migrateAndSeedAsync();
        logger.LogInformation("Database reset complete. Login: admin / {Password}", DatabaseSeeder.DefaultAdminPassword);
    }
}
