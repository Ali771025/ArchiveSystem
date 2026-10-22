// src/ArchiveSystem.Infrastructure/Migrations/20240101000000_CreateArchiveSystemTables.cs
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateArchiveSystemTables : Migration
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

            // Create Classification table
            migrationBuilder.CreateTable(
                name: "Classifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Classification title"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true, comment: "Parent classification ID"),
                    Path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, comment: "Materialized path for hierarchy"),
                    Level = table.Column<int>(type: "int", nullable: false, comment: "Hierarchy level"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, comment: "Whether classification is active"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, comment: "Classification description"),
                    ColorCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, comment: "Color code for UI"),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Icon name"),
                    DynamicFieldGroups = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "JSON schema for dynamic field groups"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Record creation timestamp"),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Record modification timestamp"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Soft delete timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Classifications_Classifications_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Classifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Classification taxonomy for organizing cases");

            // Create Case table
            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Unique case identifier"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "Case title"),
                    ClassificationId = table.Column<long>(type: "bigint", nullable: false, comment: "Associated classification ID"),
                    Owners = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "JSON array of case owners"),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Physical location"),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Case closure timestamp"),
                    DynamicFields = table.Column<string>(type: "nvarchar(max)", nullable: false, comment: "JSON for dynamic fields"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Record creation timestamp"),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Record modification timestamp"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Soft delete timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cases_Classifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "Classifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Document cases for organizing related files");

            // Create partitioned Files table
            migrationBuilder.Sql(
                $$"""
                IF OBJECT_ID(N'[dbo].[Files]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[Files] (
                        [Id] bigint IDENTITY(1,1) NOT NULL,
                        [Name] nvarchar(255) NOT NULL,
                        [Size] bigint NOT NULL,
                        [Extension] nvarchar(10) NOT NULL,
                        [ContentType] nvarchar(100) NOT NULL,
                        [StoragePath] nvarchar(1000) NOT NULL,
                        [Status] tinyint NOT NULL,
                        [CaseId] bigint NULL,
                        [Thumbnail] varbinary(max) NULL,
                        [OcrResult] nvarchar(max) NULL,
                        [Tags] nvarchar(max) NULL,
                        [Hash] nvarchar(255) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [ModifiedAt] datetime2 NULL,
                        [DeletedAt] datetime2 NULL,
                        CONSTRAINT [PK_Files] PRIMARY KEY NONCLUSTERED ([Id]),
                        [PartitionKey] AS CAST([CreatedAt] AS date)
                    ) ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                      AND name = N'IX_Files_CreatedAt'
                )
                BEGIN
                    CREATE CLUSTERED INDEX [IX_Files_CreatedAt]
                        ON [dbo].[Files] ([CreatedAt] DESC)
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                      AND name = N'IX_Files_CaseId'
                )
                BEGIN
                    CREATE INDEX [IX_Files_CaseId]
                        ON [dbo].[Files] ([CaseId])
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                      AND name = N'IX_Files_Status'
                )
                BEGIN
                    CREATE INDEX [IX_Files_Status]
                        ON [dbo].[Files] ([Status])
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                      AND name = N'IX_Files_Extension'
                )
                BEGIN
                    CREATE INDEX [IX_Files_Extension]
                        ON [dbo].[Files] ([Extension])
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[dbo].[Files]', N'U')
                      AND name = N'IX_Files_Tags'
                )
                BEGIN
                    CREATE INDEX [IX_Files_Tags]
                        ON [dbo].[Files] ([Tags])
                        ON [{{PartitionSchemeName}}]([CreatedAt]);
                END;
                """);

            // Create AuditLog table
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false, comment: "User ID who performed action"),
                    UserName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, comment: "Username"),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Action performed"),
                    TableName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Table affected"),
                    RecordId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Record ID affected"),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "Previous value"),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "New value"),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false, comment: "IP address of request"),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, comment: "User agent string"),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, comment: "Log severity level"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Log creation timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                },
                comment: "Audit trail for all system actions");

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Cases_CaseNumber",
                table: "Cases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_ClassificationId",
                table: "Cases",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_IsActive",
                table: "Classifications",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_ParentId",
                table: "Classifications",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Classifications_Path",
                table: "Classifications",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TableName",
                table: "AuditLogs",
                column: "TableName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Cases");

            migrationBuilder.DropTable(
                name: "Classifications");

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
