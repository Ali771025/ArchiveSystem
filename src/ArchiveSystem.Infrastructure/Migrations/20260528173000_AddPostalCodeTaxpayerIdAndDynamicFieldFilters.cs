using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class AddPostalCodeTaxpayerIdAndDynamicFieldFilters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Cases",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FilterType",
                table: "DynamicFieldDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "ShowAsFilter",
                table: "DynamicFieldDefinitions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaxpayerId",
                table: "Owners",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Dossiers]
                SET [PostalCode] = NULL
                WHERE [PostalCode] IS NOT NULL
                  AND ([PostalCode] LIKE '%[^0-9]%' OR LEN([PostalCode]) <> 10);
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Dossiers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "FilterType",
                table: "DynamicFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "ShowAsFilter",
                table: "DynamicFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "TaxpayerId",
                table: "Owners");

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Dossiers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
