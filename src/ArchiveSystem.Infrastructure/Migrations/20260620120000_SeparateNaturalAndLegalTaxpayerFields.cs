using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260620120000_SeparateNaturalAndLegalTaxpayerFields")]
    public partial class SeparateNaturalAndLegalTaxpayerFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Taxpayers]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_NationalId' AND object_id = OBJECT_ID('[Taxpayers]'))
                    BEGIN
                        DROP INDEX [IX_Taxpayers_NationalId] ON [Taxpayers];
                    END;

                    IF COL_LENGTH('Taxpayers', 'CompanyName') IS NULL
                    BEGIN
                        ALTER TABLE [Taxpayers] ADD [CompanyName] nvarchar(100) NULL;
                    END;

                    IF COL_LENGTH('Taxpayers', 'NationalCompanyId') IS NULL
                    BEGIN
                        ALTER TABLE [Taxpayers] ADD [NationalCompanyId] nvarchar(10) NULL;
                    END;

                    ALTER TABLE [Taxpayers] ALTER COLUMN [FirstName] nvarchar(100) NULL;
                    ALTER TABLE [Taxpayers] ALTER COLUMN [LastName] nvarchar(100) NULL;
                    ALTER TABLE [Taxpayers] ALTER COLUMN [NationalId] nvarchar(10) NULL;

                    EXEC(N'
                        UPDATE [Taxpayers]
                        SET
                            [CompanyName] = COALESCE(NULLIF(LTRIM(RTRIM([CompanyName])), N''''), NULLIF(LTRIM(RTRIM([FirstName])), N'''')),
                            [NationalCompanyId] = COALESCE(NULLIF(LTRIM(RTRIM([NationalCompanyId])), N''''), NULLIF(LTRIM(RTRIM([NationalId])), N'''')),
                            [FirstName] = NULL,
                            [LastName] = NULL,
                            [NationalId] = NULL,
                            [IdentityNumber] = NULL
                        WHERE [TaxpayerType] = 2;
                    ');

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_NationalId' AND object_id = OBJECT_ID('[Taxpayers]'))
                    BEGIN
                        EXEC(N'CREATE UNIQUE INDEX [IX_Taxpayers_NationalId] ON [Taxpayers]([NationalId]) WHERE [NationalId] IS NOT NULL AND [DeletedAt] IS NULL;');
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_NationalCompanyId' AND object_id = OBJECT_ID('[Taxpayers]'))
                    BEGIN
                        EXEC(N'CREATE UNIQUE INDEX [IX_Taxpayers_NationalCompanyId] ON [Taxpayers]([NationalCompanyId]) WHERE [NationalCompanyId] IS NOT NULL AND [DeletedAt] IS NULL;');
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_CompanyName' AND object_id = OBJECT_ID('[Taxpayers]'))
                    BEGIN
                        EXEC(N'CREATE INDEX [IX_Taxpayers_CompanyName] ON [Taxpayers]([CompanyName]);');
                    END;
                END;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Taxpayers]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_NationalCompanyId' AND object_id = OBJECT_ID('[Taxpayers]'))
                    BEGIN
                        DROP INDEX [IX_Taxpayers_NationalCompanyId] ON [Taxpayers];
                    END;

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_CompanyName' AND object_id = OBJECT_ID('[Taxpayers]'))
                    BEGIN
                        DROP INDEX [IX_Taxpayers_CompanyName] ON [Taxpayers];
                    END;

                    IF COL_LENGTH('Taxpayers', 'CompanyName') IS NOT NULL
                       AND COL_LENGTH('Taxpayers', 'NationalCompanyId') IS NOT NULL
                    BEGIN
                        EXEC(N'
                            UPDATE [Taxpayers]
                            SET
                                [FirstName] = COALESCE(NULLIF(LTRIM(RTRIM([FirstName])), N''''), NULLIF(LTRIM(RTRIM([CompanyName])), N''''), N''''),
                                [LastName] = COALESCE([LastName], N''-''),
                                [NationalId] = COALESCE(NULLIF(LTRIM(RTRIM([NationalId])), N''''), NULLIF(LTRIM(RTRIM([NationalCompanyId])), N''''), N'''')
                            WHERE [TaxpayerType] = 2;
                        ');
                    END;

                    UPDATE [Taxpayers]
                    SET
                        [FirstName] = COALESCE([FirstName], N''),
                        [LastName] = COALESCE([LastName], N''),
                        [NationalId] = COALESCE([NationalId], N'')
                    WHERE [TaxpayerType] = 1;

                    IF COL_LENGTH('Taxpayers', 'NationalCompanyId') IS NOT NULL
                    BEGIN
                        ALTER TABLE [Taxpayers] DROP COLUMN [NationalCompanyId];
                    END;

                    IF COL_LENGTH('Taxpayers', 'CompanyName') IS NOT NULL
                    BEGIN
                        ALTER TABLE [Taxpayers] DROP COLUMN [CompanyName];
                    END;

                    ALTER TABLE [Taxpayers] ALTER COLUMN [FirstName] nvarchar(100) NOT NULL;
                    ALTER TABLE [Taxpayers] ALTER COLUMN [LastName] nvarchar(100) NOT NULL;
                    ALTER TABLE [Taxpayers] ALTER COLUMN [NationalId] nvarchar(10) NOT NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Taxpayers_NationalId' AND object_id = OBJECT_ID('[Taxpayers]'))
                    BEGIN
                        EXEC(N'CREATE UNIQUE INDEX [IX_Taxpayers_NationalId] ON [Taxpayers]([NationalId]) WHERE [DeletedAt] IS NULL;');
                    END;
                END;
                """);
        }
    }
}
