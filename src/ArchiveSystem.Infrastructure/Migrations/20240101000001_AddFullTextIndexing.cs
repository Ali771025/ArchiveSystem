// src/ArchiveSystem.Infrastructure/Migrations/20240101000001_AddFullTextIndexing.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFullTextIndexing : Migration
    {
        private const string FullTextCatalogName = "FTS_Catalog";
        private const string FullTextKeyIndexName = "IX_Files_Id";
        private const string SearchProcedureName = "sp_SearchFilesByContent";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $$"""
                IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM sys.fulltext_catalogs
                        WHERE name = '{{FullTextCatalogName}}'
                    )
                    BEGIN
                        EXEC sys.sp_executesql N'CREATE FULLTEXT CATALOG [{{FullTextCatalogName}}] AS DEFAULT;';
                    END;

                    IF OBJECT_ID(N'[dbo].[Files]', N'U') IS NOT NULL
                       AND NOT EXISTS (
                           SELECT 1
                           FROM sys.fulltext_indexes
                           WHERE object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                       )
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1
                            FROM sys.indexes
                            WHERE name = N'{{FullTextKeyIndexName}}'
                              AND object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                        )
                        BEGIN
                            EXEC sys.sp_executesql N'CREATE UNIQUE NONCLUSTERED INDEX [{{FullTextKeyIndexName}}] ON [dbo].[Files]([Id]);';
                        END;

                        EXEC sys.sp_executesql N'
                            CREATE FULLTEXT INDEX ON [dbo].[Files]([OcrResult] LANGUAGE 1033)
                            KEY INDEX [{{FullTextKeyIndexName}}]
                            ON [{{FullTextCatalogName}}]
                            WITH CHANGE_TRACKING AUTO;';
                    END;
                END;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                $$"""
                IF OBJECT_ID(N'[dbo].[{{SearchProcedureName}}]', N'P') IS NULL
                BEGIN
                    EXEC sys.sp_executesql N'
                        CREATE PROCEDURE [dbo].[{{SearchProcedureName}}]
                            @SearchTerm NVARCHAR(1000),
                            @PageIndex INT = 1,
                            @PageSize INT = 20,
                            @TotalRecords INT OUTPUT
                        AS
                        BEGIN
                            SET NOCOUNT ON;
                            SELECT 1;
                        END;';
                END;
                """);

            migrationBuilder.Sql(
                $$"""
                ALTER PROCEDURE [dbo].[{{SearchProcedureName}}]
                    @SearchTerm NVARCHAR(1000),
                    @PageIndex INT = 1,
                    @PageSize INT = 20,
                    @TotalRecords INT OUTPUT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Get total count
                    SELECT @TotalRecords = COUNT(*)
                    FROM [dbo].[Files]
                    WHERE CONTAINS([OcrResult], @SearchTerm)
                      AND [DeletedAt] IS NULL;

                    -- Get paged results
                    SELECT
                        [Id],
                        [Name],
                        [Size],
                        [Extension],
                        [ContentType],
                        [StoragePath],
                        [Status],
                        [CaseId],
                        [CreatedAt],
                        [ModifiedAt]
                    FROM [dbo].[Files]
                    WHERE CONTAINS([OcrResult], @SearchTerm)
                      AND [DeletedAt] IS NULL
                    ORDER BY [CreatedAt] DESC
                    OFFSET (@PageIndex - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $$"""
                IF OBJECT_ID(N'[dbo].[{{SearchProcedureName}}]', N'P') IS NOT NULL
                BEGIN
                    DROP PROCEDURE [dbo].[{{SearchProcedureName}}];
                END;
                """);

            migrationBuilder.Sql(
                """
                IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                   AND OBJECT_ID(N'[dbo].[Files]', N'U') IS NOT NULL
                   AND EXISTS (
                       SELECT 1
                       FROM sys.fulltext_indexes
                       WHERE object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                   )
                BEGIN
                    DROP FULLTEXT INDEX ON [dbo].[Files];
                END;
                """,
                suppressTransaction: true);

            migrationBuilder.Sql(
                $$"""
                IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
                   AND EXISTS (
                       SELECT 1
                       FROM sys.fulltext_catalogs
                       WHERE name = '{{FullTextCatalogName}}'
                   )
                BEGIN
                    DROP FULLTEXT CATALOG [{{FullTextCatalogName}}];
                END;
                """,
                suppressTransaction: true);
        }
    }
}
