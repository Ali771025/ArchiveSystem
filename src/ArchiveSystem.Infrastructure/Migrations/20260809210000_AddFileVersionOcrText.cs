using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260809210000_AddFileVersionOcrText")]
    public partial class AddFileVersionOcrText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.FileVersions', N'OcrText') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[FileVersions]
                    ADD [OcrText] nvarchar(max) NULL;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.FileVersions', N'OcrText') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[FileVersions]
                    DROP COLUMN [OcrText];
                END;
                """);
        }
    }
}
