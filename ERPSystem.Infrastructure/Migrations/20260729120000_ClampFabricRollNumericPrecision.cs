using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

/// <summary>
/// FabricRolls length/cost columns were unconstrained numeric. High-scale values
/// (e.g. float yard→meter leftovers) make SUM(...) return scale &gt; 28 and crash
/// Npgsql with OverflowException when reading into System.Decimal.
/// </summary>
[DbContext(typeof(ErpDbContext))]
[Migration("20260729120000_ClampFabricRollNumericPrecision")]
public partial class ClampFabricRollNumericPrecision : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "FabricRolls"
            SET
                "LengthMeters" = ROUND("LengthMeters", 4),
                "RemainingLengthMeters" = ROUND("RemainingLengthMeters", 4),
                "CostPerMeter" = ROUND("CostPerMeter", 4),
                "SalePricePerMeter" = CASE
                    WHEN "SalePricePerMeter" IS NULL THEN NULL
                    ELSE ROUND("SalePricePerMeter", 4)
                END,
                "WeightKg" = CASE
                    WHEN "WeightKg" IS NULL THEN NULL
                    ELSE ROUND("WeightKg", 4)
                END;

            ALTER TABLE "FabricRolls"
                ALTER COLUMN "LengthMeters" TYPE numeric(18,4)
                    USING ROUND("LengthMeters", 4),
                ALTER COLUMN "RemainingLengthMeters" TYPE numeric(18,4)
                    USING ROUND("RemainingLengthMeters", 4),
                ALTER COLUMN "CostPerMeter" TYPE numeric(18,4)
                    USING ROUND("CostPerMeter", 4),
                ALTER COLUMN "SalePricePerMeter" TYPE numeric(18,4)
                    USING CASE
                        WHEN "SalePricePerMeter" IS NULL THEN NULL
                        ELSE ROUND("SalePricePerMeter", 4)
                    END,
                ALTER COLUMN "WeightKg" TYPE numeric(18,4)
                    USING CASE
                        WHEN "WeightKg" IS NULL THEN NULL
                        ELSE ROUND("WeightKg", 4)
                    END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "FabricRolls"
                ALTER COLUMN "LengthMeters" TYPE numeric
                    USING "LengthMeters",
                ALTER COLUMN "RemainingLengthMeters" TYPE numeric
                    USING "RemainingLengthMeters",
                ALTER COLUMN "CostPerMeter" TYPE numeric
                    USING "CostPerMeter",
                ALTER COLUMN "SalePricePerMeter" TYPE numeric
                    USING "SalePricePerMeter",
                ALTER COLUMN "WeightKg" TYPE numeric
                    USING "WeightKg";
            """);
    }
}
