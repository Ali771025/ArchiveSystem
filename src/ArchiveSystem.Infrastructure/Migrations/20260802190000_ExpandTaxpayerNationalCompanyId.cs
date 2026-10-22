using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260802190000_ExpandTaxpayerNationalCompanyId")]
    public partial class ExpandTaxpayerNationalCompanyId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Taxpayers_NationalCompanyId",
                table: "Taxpayers");

            migrationBuilder.AlterColumn<string>(
                name: "NationalCompanyId",
                table: "Taxpayers",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Taxpayers_NationalCompanyId",
                table: "Taxpayers",
                column: "NationalCompanyId",
                unique: true,
                filter: "[NationalCompanyId] IS NOT NULL AND [DeletedAt] IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Taxpayers_NationalCompanyId",
                table: "Taxpayers");

            migrationBuilder.AlterColumn<string>(
                name: "NationalCompanyId",
                table: "Taxpayers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Taxpayers_NationalCompanyId",
                table: "Taxpayers",
                column: "NationalCompanyId",
                unique: true,
                filter: "[NationalCompanyId] IS NOT NULL AND [DeletedAt] IS NULL");
        }
    }
}
