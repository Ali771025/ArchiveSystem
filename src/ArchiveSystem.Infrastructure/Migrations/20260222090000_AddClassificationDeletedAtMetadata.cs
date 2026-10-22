using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    public partial class AddClassificationDeletedAtMetadata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Classifications', 'DeletedAt') IS NULL
                BEGIN
                    ALTER TABLE [Classifications] ADD [DeletedAt] datetime2 NULL;
                END;

                IF COL_LENGTH('Classifications', 'IsActive') IS NOT NULL
                   AND COL_LENGTH('Classifications', 'DeletedAt') IS NOT NULL
                BEGIN
                    UPDATE [Classifications]
                    SET [DeletedAt] = COALESCE([DeletedAt], [ModifiedAt], [CreatedAt], SYSUTCDATETIME())
                    WHERE [IsActive] = 0
                      AND [DeletedAt] IS NULL;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Classifications_DeletedAt'
                      AND object_id = OBJECT_ID('Classifications'))
                BEGIN
                    CREATE INDEX [IX_Classifications_DeletedAt] ON [Classifications]([DeletedAt]);
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = 'IX_Classifications_DeletedAt'
                      AND object_id = OBJECT_ID('Classifications'))
                BEGIN
                    DROP INDEX [IX_Classifications_DeletedAt] ON [Classifications];
                END;

                IF COL_LENGTH('Classifications', 'DeletedAt') IS NOT NULL
                BEGIN
                    ALTER TABLE [Classifications] DROP COLUMN [DeletedAt];
                END;
                """);
        }
    }
}
