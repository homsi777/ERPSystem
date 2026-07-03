using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERPSystem.Infrastructure.Migrations;

public partial class AddJournalBooksAndGlIntegration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "journal_books",
            schema: "accounting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<string>(type: "text", nullable: false),
                NameAr = table.Column<string>(type: "text", nullable: false),
                NameEn = table.Column<string>(type: "text", nullable: false),
                BookType = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_journal_books", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_journal_books_CompanyId_Code",
            schema: "accounting",
            table: "journal_books",
            columns: new[] { "CompanyId", "Code" },
            unique: true);

        migrationBuilder.AddColumn<Guid>(
            name: "JournalBookId",
            schema: "accounting",
            table: "journal_entries",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_journal_entries_JournalBookId",
            schema: "accounting",
            table: "journal_entries",
            column: "JournalBookId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_journal_entries_JournalBookId",
            schema: "accounting",
            table: "journal_entries");

        migrationBuilder.DropColumn(
            name: "JournalBookId",
            schema: "accounting",
            table: "journal_entries");

        migrationBuilder.DropTable(
            name: "journal_books",
            schema: "accounting");
    }
}
