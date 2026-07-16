using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

[DbContext(typeof(ErpDbContext))]
[Migration("20260727120000_AddUserSessions")]
public partial class AddUserSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS identity.user_sessions (
                "Id" uuid NOT NULL PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "Username" character varying(100) NOT NULL,
                "FullNameAr" character varying(200) NOT NULL,
                "ClientType" integer NOT NULL,
                "RefreshTokenHash" character varying(128) NULL,
                "DeviceInfo" character varying(500) NULL,
                "IpAddress" character varying(64) NULL,
                "LoginAt" timestamp with time zone NOT NULL,
                "LogoutAt" timestamp with time zone NULL,
                "LastSeenAt" timestamp with time zone NULL,
                "ExpiresAt" timestamp with time zone NOT NULL,
                "IsRevoked" boolean NOT NULL DEFAULT false,
                "RevokedReason" character varying(100) NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_user_sessions_UserId"
                ON identity.user_sessions ("UserId");

            CREATE INDEX IF NOT EXISTS "IX_user_sessions_RefreshTokenHash"
                ON identity.user_sessions ("RefreshTokenHash");

            CREATE INDEX IF NOT EXISTS "IX_user_sessions_LoginAt"
                ON identity.user_sessions ("LoginAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS identity.user_sessions;");
    }
}
