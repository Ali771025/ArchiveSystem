// src/ArchiveSystem.Infrastructure/Migrations/20240101000001_AddTablePartitioning.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTablePartitioning : Migration
    {
        private const string PartitionFunctionName = "FilePartitionFunction";
        private const string PartitionSchemeName = "FilePartitionScheme";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server supports these DDL statements inside EF Core migration transactions,
            // so suppressTransaction is intentionally not used.
            migrationBuilder.Sql(
                $$"""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.partition_functions
                    WHERE name = '{{PartitionFunctionName}}'
                )
                BEGIN
                    EXEC sys.sp_executesql N'
                        CREATE PARTITION FUNCTION [{{PartitionFunctionName}}] (datetime2)
                        AS RANGE RIGHT
                        FOR VALUES
                        (
                            -- 2024 monthly boundaries
                            ''2024-01-01'', ''2024-02-01'', ''2024-03-01'', ''2024-04-01'',
                            ''2024-05-01'', ''2024-06-01'', ''2024-07-01'', ''2024-08-01'',
                            ''2024-09-01'', ''2024-10-01'', ''2024-11-01'', ''2024-12-01'',

                            -- 2025 monthly boundaries
                            ''2025-01-01'', ''2025-02-01'', ''2025-03-01'', ''2025-04-01'',
                            ''2025-05-01'', ''2025-06-01'', ''2025-07-01'', ''2025-08-01'',
                            ''2025-09-01'', ''2025-10-01'', ''2025-11-01'', ''2025-12-01'',

                            -- 2026 monthly boundaries
                            ''2026-01-01'', ''2026-02-01'', ''2026-03-01'', ''2026-04-01'',
                            ''2026-05-01'', ''2026-06-01'', ''2026-07-01'', ''2026-08-01'',
                            ''2026-09-01'', ''2026-10-01'', ''2026-11-01'', ''2026-12-01'',

                            -- 2027 monthly boundaries
                            ''2027-01-01'', ''2027-02-01'', ''2027-03-01'', ''2027-04-01'',
                            ''2027-05-01'', ''2027-06-01'', ''2027-07-01'', ''2027-08-01'',
                            ''2027-09-01'', ''2027-10-01'', ''2027-11-01'', ''2027-12-01'',

                            -- 2028 monthly boundaries
                            ''2028-01-01'', ''2028-02-01'', ''2028-03-01'', ''2028-04-01'',
                            ''2028-05-01'', ''2028-06-01'', ''2028-07-01'', ''2028-08-01'',
                            ''2028-09-01'', ''2028-10-01'', ''2028-11-01'', ''2028-12-01'',

                            -- 2029 monthly boundaries
                            ''2029-01-01'', ''2029-02-01'', ''2029-03-01'', ''2029-04-01'',
                            ''2029-05-01'', ''2029-06-01'', ''2029-07-01'', ''2029-08-01'',
                            ''2029-09-01'', ''2029-10-01'', ''2029-11-01'', ''2029-12-01'',

                            -- 2030 monthly boundaries
                            ''2030-01-01'', ''2030-02-01'', ''2030-03-01'', ''2030-04-01'',
                            ''2030-05-01'', ''2030-06-01'', ''2030-07-01'', ''2030-08-01'',
                            ''2030-09-01'', ''2030-10-01'', ''2030-11-01'', ''2030-12-01''
                        );';
                END;
                """);

            migrationBuilder.Sql(
                $$"""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.partition_schemes
                    WHERE name = '{{PartitionSchemeName}}'
                )
                BEGIN
                    EXEC sys.sp_executesql N'
                        CREATE PARTITION SCHEME [{{PartitionSchemeName}}]
                        AS PARTITION [{{PartitionFunctionName}}]
                        ALL TO ([PRIMARY]);';
                END;
                """);

            // Create partitioned table
            migrationBuilder.Sql(
                $$"""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.tables
                    WHERE name = N'PartitionedFiles'
                      AND schema_id = SCHEMA_ID(N'dbo')
                )
                BEGIN
                    CREATE TABLE [dbo].[PartitionedFiles] (
                        [Id] bigint IDENTITY(1,1) NOT NULL,
                        [Name] nvarchar(255) NOT NULL,
                        [Size] bigint NOT NULL,
                        [Extension] nvarchar(10) NOT NULL,
                        [ContentType] nvarchar(100) NOT NULL,
                        [StoragePath] nvarchar(1000) NOT NULL,
                        [Status] tinyint NOT NULL,
                        [CaseId] bigint NULL,
                        [DeletedAt] datetime2 NULL,
                        [Hash] nvarchar(255) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ModifiedAt] datetime2 NULL,
                        CONSTRAINT [PK_PartitionedFiles] PRIMARY KEY NONCLUSTERED ([Id]),
                        [PartitionKey] AS CAST([CreatedAt] AS date)
                    ) ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[PartitionedFiles]', N'U')
                      AND name = N'IX_PartitionedFiles_CreatedAt'
                )
                BEGIN
                    CREATE CLUSTERED INDEX [IX_PartitionedFiles_CreatedAt]
                        ON [dbo].[PartitionedFiles] ([CreatedAt] DESC)
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[PartitionedFiles]', N'U')
                      AND name = N'IX_PartitionedFiles_CaseId'
                )
                BEGIN
                    CREATE INDEX [IX_PartitionedFiles_CaseId]
                        ON [dbo].[PartitionedFiles] ([CaseId])
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[PartitionedFiles]', N'U')
                      AND name = N'IX_PartitionedFiles_Status'
                )
                BEGIN
                    CREATE INDEX [IX_PartitionedFiles_Status]
                        ON [dbo].[PartitionedFiles] ([Status])
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[PartitionedFiles]', N'U')
                      AND name = N'IX_PartitionedFiles_Extension'
                )
                BEGIN
                    CREATE INDEX [IX_PartitionedFiles_Extension]
                        ON [dbo].[PartitionedFiles] ([Extension])
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;
                """);

            // Create a view to make partitioned table look like regular table
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.views
                    WHERE name = N'vw_Files'
                      AND schema_id = SCHEMA_ID(N'dbo')
                )
                BEGIN
                    EXEC sys.sp_executesql N'CREATE VIEW [dbo].[vw_Files] AS SELECT * FROM [dbo].[PartitionedFiles];';
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.views
                    WHERE name = N'vw_Files'
                      AND schema_id = SCHEMA_ID(N'dbo')
                )
                BEGIN
                    DROP VIEW [dbo].[vw_Files];
                END;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[PartitionedFiles]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[PartitionedFiles];
                END;
                """);

            migrationBuilder.Sql(
                $$"""
                IF EXISTS (
                    SELECT 1
                    FROM sys.partition_schemes
                    WHERE name = '{{PartitionSchemeName}}'
                )
                BEGIN
                    DROP PARTITION SCHEME [{{PartitionSchemeName}}];
                END;
                """);

            migrationBuilder.Sql(
                $$"""
                IF EXISTS (
                    SELECT 1
                    FROM sys.partition_functions
                    WHERE name = '{{PartitionFunctionName}}'
                )
                BEGIN
                    DROP PARTITION FUNCTION [{{PartitionFunctionName}}];
                END;
                """);
        }
    }
}
