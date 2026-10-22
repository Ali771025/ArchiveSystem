using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260705120000_AddArchivedCasesSystemClassification")]
    public partial class AddArchivedCasesSystemClassification : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SystemKey",
                table: "Classifications",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Classifications_SystemKey",
                table: "Classifications",
                column: "SystemKey",
                unique: true,
                filter: "[SystemKey] IS NOT NULL");

            migrationBuilder.Sql(
                """
                DECLARE @ArchiveId bigint;

                SELECT TOP (1) @ArchiveId = [Id]
                FROM [Classifications]
                WHERE [SystemKey] = N'ArchivedCases';

                IF @ArchiveId IS NULL
                BEGIN
                    SELECT TOP (1) @ArchiveId = [Id]
                    FROM [Classifications]
                    WHERE [ParentId] IS NULL AND [Title] = N'بایگانی راکد'
                    ORDER BY CASE WHEN [DeletedAt] IS NULL THEN 0 ELSE 1 END, [Id];
                END;

                IF @ArchiveId IS NULL
                BEGIN
                    INSERT INTO [Classifications]
                        ([Title], [ParentId], [Path], [Level], [IsActive], [DeletedAt], [SystemKey],
                         [Description], [ColorCode], [Icon], [DynamicFieldGroups], [CreatedAt], [ModifiedAt])
                    VALUES
                        (N'بایگانی راکد', NULL, N'/', 0, 1, NULL, N'ArchivedCases',
                         N'', N'#000000', N'folder', N'{}', SYSUTCDATETIME(), NULL);
                END
                ELSE
                BEGIN
                    UPDATE [Classifications]
                    SET [Title] = N'بایگانی راکد',
                        [ParentId] = NULL,
                        [Path] = N'/',
                        [Level] = 0,
                        [IsActive] = 1,
                        [DeletedAt] = NULL,
                        [SystemKey] = N'ArchivedCases',
                        [ModifiedAt] = SYSUTCDATETIME()
                    WHERE [Id] = @ArchiveId;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Classifications]
                SET [SystemKey] = NULL
                WHERE [SystemKey] = N'ArchivedCases';
                """);

            migrationBuilder.DropIndex(
                name: "UX_Classifications_SystemKey",
                table: "Classifications");

            migrationBuilder.DropColumn(
                name: "SystemKey",
                table: "Classifications");
        }
    }
}
