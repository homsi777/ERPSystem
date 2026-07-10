using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ERPSystem.Infrastructure.E2E;

/// <summary>
/// Blocks E2E write tools when targeting production database, production company,
/// non-TEST company names, or Production environment. Baseline read-only tools
/// call <see cref="GuardWritableE2E"/> only when persisting test data.
/// </summary>
public static class E2EProductionGuard
{
    public const string ProductionDatabaseName = "erp_pro";
    public const string Phase3TestDatabaseName = "erp_pro_phase3_e2e";

    public static void GuardWritableE2E(string? connectionString = null, Guid? companyId = null)
    {
        var cs = connectionString
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? Environment.GetEnvironmentVariable("ERP_CONNECTION_STRING")
                 ?? "";

        if (!string.IsNullOrWhiteSpace(cs))
        {
            var builder = new NpgsqlConnectionStringBuilder(cs);
            if (string.Equals(builder.Database, ProductionDatabaseName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Refusing E2E write operations on production database '{ProductionDatabaseName}'. " +
                    $"Use isolated database '{Phase3TestDatabaseName}'.");

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                      ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Refusing E2E write operations in Production environment.");
        }

        if (companyId is Guid id && id == DatabaseSeeder.DefaultCompanyId)
            throw new InvalidOperationException("Refusing E2E write operations on production company.");
    }

    public static async Task GuardWritableE2EAsync(
        ErpDbContext context,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var cs = context.Database.GetConnectionString();
        GuardWritableE2E(cs, companyId);

        if (companyId is Guid id)
        {
            var company = await context.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (company is not null
                && !company.NameEn.Contains("TEST", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Refusing E2E write on non-TEST company '{company.NameEn}'.");
            }
        }
    }

    public static string ResolvePhase3TestConnection(string? configuredConnection)
    {
        if (string.IsNullOrWhiteSpace(configuredConnection))
            throw new InvalidOperationException("Connection string is required for Phase 3 E2E.");

        var builder = new NpgsqlConnectionStringBuilder(configuredConnection)
        {
            Database = Phase3TestDatabaseName
        };
        return builder.ConnectionString;
    }
}
