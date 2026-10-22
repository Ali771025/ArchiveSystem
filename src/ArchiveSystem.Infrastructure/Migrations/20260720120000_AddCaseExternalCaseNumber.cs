using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260720120000_AddCaseExternalCaseNumber")]
    public partial class AddCaseExternalCaseNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalCaseNumber",
                table: "Cases",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_ExternalCaseNumber",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ExternalCaseNumber",
                table: "Cases");
        }
    }
}
