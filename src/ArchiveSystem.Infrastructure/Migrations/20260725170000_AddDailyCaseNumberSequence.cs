using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260725170000_AddDailyCaseNumberSequence")]
    public partial class AddDailyCaseNumberSequence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_ExternalCaseNumber",
                table: "Cases");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalCaseNumber",
                table: "Cases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "CaseNumberDailySequences",
                columns: table => new
                {
                    DateKey = table.Column<string>(type: "char(8)", maxLength: 8, nullable: false),
                    LastSerial = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseNumberDailySequences", x => x.DateKey);
                    table.CheckConstraint(
                        "CK_CaseNumberDailySequences_LastSerial",
                        "[LastSerial] >= 1 AND [LastSerial] <= 99999");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ExternalCaseNumber",
                table: "Cases",
                column: "ExternalCaseNumber",
                unique: true,
                filter: "[ExternalCaseNumber] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CaseNumberDailySequences");

            migrationBuilder.DropIndex(
                name: "IX_Cases_ExternalCaseNumber",
                table: "Cases");

            migrationBuilder.Sql(
                "UPDATE [Cases] SET [ExternalCaseNumber] = [CaseNumber] WHERE [ExternalCaseNumber] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalCaseNumber",
                table: "Cases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ExternalCaseNumber",
                table: "Cases",
                column: "ExternalCaseNumber",
                unique: true);
        }
    }
}
