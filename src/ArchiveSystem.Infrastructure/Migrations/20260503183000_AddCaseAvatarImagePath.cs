using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class AddCaseAvatarImagePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Cases', 'AvatarImagePath') IS NULL
                BEGIN
                    ALTER TABLE [Cases] ADD [AvatarImagePath] nvarchar(500) NULL;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Cases', 'AvatarImagePath') IS NOT NULL
                BEGIN
                    ALTER TABLE [Cases] DROP COLUMN [AvatarImagePath];
                END;
                """);
        }
    }
}
