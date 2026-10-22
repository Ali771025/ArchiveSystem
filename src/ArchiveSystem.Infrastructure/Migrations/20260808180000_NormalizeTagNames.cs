using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ArchiveSystem.Infrastructure.Persistence;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260808180000_NormalizeTagNames")]
    public partial class NormalizeTagNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Tags",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE Tags
                SET NormalizedName = LOWER(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(Name, N'ي', N'ی'), N'ى', N'ی'), N'ك', N'ک'))));
                WHILE EXISTS (SELECT 1 FROM Tags WHERE NormalizedName LIKE N'%  %')
                    UPDATE Tags SET NormalizedName = REPLACE(NormalizedName, N'  ', N' ');

                ;WITH Ranked AS (
                    SELECT Id, MIN(Id) OVER (PARTITION BY NormalizedName) AS KeepId
                    FROM Tags
                ), Duplicates AS (
                    SELECT Id, KeepId FROM Ranked WHERE Id <> KeepId
                )
                DELETE ft
                FROM FileTags ft
                INNER JOIN Duplicates d ON d.Id = ft.TagId
                WHERE EXISTS (SELECT 1 FROM FileTags keeper WHERE keeper.FileId = ft.FileId AND keeper.TagId = d.KeepId);

                ;WITH Ranked AS (
                    SELECT Id, MIN(Id) OVER (PARTITION BY NormalizedName) AS KeepId
                    FROM Tags
                )
                UPDATE ft SET TagId = ranked.KeepId
                FROM FileTags ft
                INNER JOIN Ranked ranked ON ranked.Id = ft.TagId
                WHERE ranked.Id <> ranked.KeepId;

                ;WITH Ranked AS (
                    SELECT Id, ROW_NUMBER() OVER (PARTITION BY NormalizedName ORDER BY Id) AS RowNumber
                    FROM Tags
                )
                DELETE tags FROM Tags tags INNER JOIN Ranked ranked ON ranked.Id = tags.Id WHERE ranked.RowNumber > 1;
                """);

            migrationBuilder.DropIndex(name: "UX_Tags_Name", table: "Tags");
            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                table: "Tags",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);
            migrationBuilder.CreateIndex(
                name: "UX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "UX_Tags_NormalizedName", table: "Tags");
            migrationBuilder.DropColumn(name: "NormalizedName", table: "Tags");
            migrationBuilder.CreateIndex(name: "UX_Tags_Name", table: "Tags", column: "Name", unique: true);
        }
    }
}
