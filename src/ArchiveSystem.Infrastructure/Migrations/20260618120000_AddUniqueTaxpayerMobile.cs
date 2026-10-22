using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class AddUniqueTaxpayerMobile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Taxpayers_Mobile",
                table: "Taxpayers",
                column: "Mobile",
                unique: true,
                filter: "[Mobile] IS NOT NULL AND [DeletedAt] IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Taxpayers_Mobile",
                table: "Taxpayers");
        }
    }
}
