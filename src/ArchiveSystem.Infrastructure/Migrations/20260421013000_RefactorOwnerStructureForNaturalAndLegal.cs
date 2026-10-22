using ArchiveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchiveSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ArchiveDbContext))]
    [Migration("20260421013000_RefactorOwnerStructureForNaturalAndLegal")]
    public partial class RefactorOwnerStructureForNaturalAndLegal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Owners]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Owners', 'OwnerType') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [OwnerType] int NOT NULL CONSTRAINT [DF_Owners_OwnerType] DEFAULT (1);
                    END;

                    IF COL_LENGTH('Owners', 'Mobile') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [Mobile] nvarchar(20) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'PostalCode') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [PostalCode] nvarchar(20) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'Address') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [Address] nvarchar(1000) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'HomePhone') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [HomePhone] nvarchar(20) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'FirstName') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [FirstName] nvarchar(100) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'LastName') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [LastName] nvarchar(100) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'IdentityNumber') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [IdentityNumber] nvarchar(50) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'CompanyName') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [CompanyName] nvarchar(200) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'NationalId') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [NationalId] nvarchar(11) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'RegistrationNumber') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [RegistrationNumber] nvarchar(50) NULL;
                    END;
                END;
""");

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Owners]', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Owners_FullName' AND object_id = OBJECT_ID('[Owners]'))
                    BEGIN
                        DROP INDEX [IX_Owners_FullName] ON [Owners];
                    END;

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Owners_NationalCode' AND object_id = OBJECT_ID('[Owners]'))
                    BEGIN
                        DROP INDEX [IX_Owners_NationalCode] ON [Owners];
                    END;
                END;
""");

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Owners]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Owners', 'FullName') IS NOT NULL
                    BEGIN
                        ALTER TABLE [Owners] ALTER COLUMN [NationalCode] nvarchar(20) NULL;
                    END;
                END;
""");

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Owners]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Owners', 'FullName') IS NOT NULL
                    BEGIN
                        UPDATE [Owners]
                        SET
                            [OwnerType] = CASE WHEN ISNULL([PersonType], 1) = 2 THEN 2 ELSE 1 END,
                            [Mobile] = [MobileNumber],
                            [HomePhone] = [PhoneNumber],
                            [FirstName] = CASE WHEN ISNULL([PersonType], 1) = 2 THEN NULL ELSE LTRIM(RTRIM([FullName])) END,
                            [LastName] = CASE WHEN ISNULL([PersonType], 1) = 2 THEN NULL ELSE N'' END,
                            [CompanyName] = CASE WHEN ISNULL([PersonType], 1) = 2 THEN LTRIM(RTRIM([FullName])) ELSE NULL END,
                            [NationalId] = CASE WHEN ISNULL([PersonType], 1) = 2 THEN LEFT(LTRIM(RTRIM([NationalCode])), 11) ELSE NULL END,
                            [NationalCode] = CASE WHEN ISNULL([PersonType], 1) = 2 THEN NULL ELSE LEFT(LTRIM(RTRIM([NationalCode])), 10) END;
                    END;
                END;
""");

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Owners]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Owners', 'FullName') IS NOT NULL
                    BEGIN
                        ALTER TABLE [Owners] DROP COLUMN [FullName];
                    END;

                    IF COL_LENGTH('Owners', 'PhoneNumber') IS NOT NULL
                    BEGIN
                        ALTER TABLE [Owners] DROP COLUMN [PhoneNumber];
                    END;

                    IF COL_LENGTH('Owners', 'MobileNumber') IS NOT NULL
                    BEGIN
                        ALTER TABLE [Owners] DROP COLUMN [MobileNumber];
                    END;

                    IF COL_LENGTH('Owners', 'PersonType') IS NOT NULL
                    BEGIN
                        ALTER TABLE [Owners] DROP COLUMN [PersonType];
                    END;
                END;
""");

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Owners]', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Owners_FirstName' AND object_id = OBJECT_ID('[Owners]'))
                    BEGIN
                        CREATE INDEX [IX_Owners_FirstName] ON [Owners]([FirstName]);
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Owners_LastName' AND object_id = OBJECT_ID('[Owners]'))
                    BEGIN
                        CREATE INDEX [IX_Owners_LastName] ON [Owners]([LastName]);
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Owners_CompanyName' AND object_id = OBJECT_ID('[Owners]'))
                    BEGIN
                        CREATE INDEX [IX_Owners_CompanyName] ON [Owners]([CompanyName]);
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Owners_NationalCode' AND object_id = OBJECT_ID('[Owners]'))
                    BEGIN
                        CREATE UNIQUE INDEX [IX_Owners_NationalCode] ON [Owners]([NationalCode]) WHERE [NationalCode] IS NOT NULL;
                    END;

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Owners_NationalId' AND object_id = OBJECT_ID('[Owners]'))
                    BEGIN
                        CREATE UNIQUE INDEX [IX_Owners_NationalId] ON [Owners]([NationalId]) WHERE [NationalId] IS NOT NULL;
                    END;
                END;
""");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[Owners]', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH('Owners', 'FullName') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [FullName] nvarchar(200) NOT NULL CONSTRAINT [DF_Owners_FullName] DEFAULT (N'');
                    END;

                    IF COL_LENGTH('Owners', 'PhoneNumber') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [PhoneNumber] nvarchar(20) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'MobileNumber') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [MobileNumber] nvarchar(20) NULL;
                    END;

                    IF COL_LENGTH('Owners', 'PersonType') IS NULL
                    BEGIN
                        ALTER TABLE [Owners] ADD [PersonType] int NOT NULL CONSTRAINT [DF_Owners_PersonType] DEFAULT (1);
                    END;

                    UPDATE [Owners]
                    SET
                        [FullName] = CASE WHEN [OwnerType] = 2 THEN COALESCE([CompanyName], N'') ELSE LTRIM(RTRIM(CONCAT(COALESCE([FirstName], N''), N' ', COALESCE([LastName], N'')))) END,
                        [NationalCode] = COALESCE([NationalCode], [NationalId], N''),
                        [PhoneNumber] = [HomePhone],
                        [MobileNumber] = [Mobile],
                        [PersonType] = CASE WHEN [OwnerType] = 2 THEN 2 ELSE 1 END;
                END;
""");
        }
    }
}
